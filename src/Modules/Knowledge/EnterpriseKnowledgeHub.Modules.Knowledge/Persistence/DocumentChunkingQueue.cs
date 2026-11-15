using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

internal sealed class DocumentChunkingQueue(
    KnowledgeDbContext db,
    TimeProvider clock,
    IOptions<DocumentChunkingOptions> chunkingOptions,
    IOptions<DocumentChunkingWorkerOptions> options) : IDocumentChunkingQueue
{
    public async Task<DocumentChunking?> ClaimAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var formats = DocumentFormat.All.Where(chunkingOptions.Value.IsEnabled).Select(format => format.Id).ToArray();

        var eligible = db.DocumentChunkings.Where(item => formats.Contains(item.FormatId) &&
            ((item.Status == DocumentChunkingStatus.Pending && item.NextAttemptAt <= now) ||
             (item.Status == DocumentChunkingStatus.Processing && item.LeaseExpiresAt <= now)));

        var candidates = await eligible
            .OrderBy(item => item.NextAttemptAt)
            .Select(item => item.Id)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var id in candidates)
        {
            var owner = Guid.NewGuid();
            var updated = await eligible
                .Where(item => item.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, DocumentChunkingStatus.Processing)
                    .SetProperty(item => item.LeaseOwner, owner)
                    .SetProperty(item => item.LeaseExpiresAt, now.AddSeconds(options.Value.LeaseSeconds))
                    .SetProperty(item => item.Attempts, item => item.Attempts + 1), cancellationToken);

            if (updated == 1)
            {
                return await db.DocumentChunkings.AsNoTracking().SingleAsync(item => item.Id == id && item.LeaseOwner == owner, cancellationToken);
            }
        }

        return null;
    }

    public async Task<int> CompleteAsync(DocumentChunking work, IReadOnlyList<DocumentChunkContent> chunks, CancellationToken cancellationToken)
    {
        ValidateChunks(work, chunks);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var updated = await WithActiveLease(work).Where(item =>
            db.Documents.Any(document => document.Id == item.DocumentId && document.OrganizationId == item.OrganizationId &&
                document.Status == DocumentStatus.Uploaded && document.ValidatedETag == item.SourceETag) &&
            db.DocumentTextExtractions.Any(extraction => extraction.Id == item.ExtractionId &&
                extraction.OrganizationId == item.OrganizationId && extraction.DocumentId == item.DocumentId &&
                extraction.SourceETag == item.SourceETag && extraction.Status == TextExtractionStatus.Completed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, DocumentChunkingStatus.Completed)
                .SetProperty(item => item.ChunkCount, chunks.Count)
                .SetProperty(item => item.CompletedAt, clock.GetUtcNow().UtcDateTime)
                .SetProperty(item => item.LastError, (string?)null)
                .SetProperty(item => item.LeaseOwner, (Guid?)null)
                .SetProperty(item => item.LeaseExpiresAt, (DateTime?)null), cancellationToken);

        if (updated == 0)
        {
            return 0;  
        } 

        var records = chunks.Select(chunk => new DocumentChunk
        {
            OrganizationId = work.OrganizationId, 
            DocumentId = work.DocumentId, 
            ChunkingId = work.Id,
            Ordinal = chunk.Ordinal, 
            Content = chunk.Content, 
            HeadingPath = chunk.HeadingPath, 
            TokenCount = chunk.TokenCount
        }).ToArray();

        db.DocumentChunks.AddRange(records);
        
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // A failed insert rolls back completion too; retries must not retain Added entities.
            foreach (var record in records)
            {
                db.Entry(record).State = EntityState.Detached;  
            } 
        }
        
        return 1;
    }

    public Task<int> FailAsync(DocumentChunking work, string errorCode, CancellationToken cancellationToken) =>
        WithActiveLease(work).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, DocumentChunkingStatus.Failed)
            .SetProperty(item => item.LastError, errorCode)
            .SetProperty(item => item.CompletedAt, clock.GetUtcNow().UtcDateTime)
            .SetProperty(item => item.LeaseOwner, (Guid?)null)
            .SetProperty(item => item.LeaseExpiresAt, (DateTime?)null), cancellationToken);

    public Task<int> RetryAsync(DocumentChunking work, string errorCode, CancellationToken cancellationToken)
    {
        var exhausted = work.Attempts >= options.Value.MaxAttempts;
        var delay = Math.Min(options.Value.RetryMaxSeconds,
            options.Value.RetryBaseSeconds * Math.Pow(2, Math.Min(work.Attempts - 1, 20)));

        return WithActiveLease(work).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, exhausted ? DocumentChunkingStatus.DeadLetter : DocumentChunkingStatus.Pending)
            .SetProperty(item => item.NextAttemptAt, clock.GetUtcNow().UtcDateTime.AddSeconds(delay))
            .SetProperty(item => item.CompletedAt, exhausted ? clock.GetUtcNow().UtcDateTime : (DateTime?)null)
            .SetProperty(item => item.LastError, errorCode)
            .SetProperty(item => item.LeaseOwner, (Guid?)null)
            .SetProperty(item => item.LeaseExpiresAt, (DateTime?)null), cancellationToken);
    }

    private void ValidateChunks(DocumentChunking work, IReadOnlyList<DocumentChunkContent> chunks)
    {
        const int maximumHeadingPathLength = 2000;

        if (chunks.Count == 0 || chunks.Count > work.MaxChunksPerDocument)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.InvalidChunks);
        }

        for (var expectedOrdinal = 0; expectedOrdinal < chunks.Count; expectedOrdinal++)
        {
            var chunk = chunks[expectedOrdinal];
            var hasInvalidOrdinal = chunk.Ordinal != expectedOrdinal;
            var hasEmptyContent = string.IsNullOrWhiteSpace(chunk.Content);
            var hasInvalidTokenCount = chunk.TokenCount <= 0 || chunk.TokenCount > work.MaxTokens;
            var hasOversizedHeadingPath = chunk.HeadingPath?.Length > maximumHeadingPathLength;

            if (hasInvalidOrdinal || hasEmptyContent || hasInvalidTokenCount || hasOversizedHeadingPath)
            {
                throw new DocumentChunkingException(DocumentChunkingErrorCodes.InvalidChunks);
            }
        }
    }

    private IQueryable<DocumentChunking> WithActiveLease(DocumentChunking work)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        return db.DocumentChunkings.Where(item => 
            item.Id == work.Id 
            && item.OrganizationId == work.OrganizationId 
            && item.DocumentId == work.DocumentId 
            && item.ExtractionId == work.ExtractionId 
            && item.SourceETag == work.SourceETag 
            && item.ConfigurationKey == work.ConfigurationKey 
            && item.Status == DocumentChunkingStatus.Processing 
            && item.LeaseOwner == work.LeaseOwner 
            && item.LeaseExpiresAt > now);
    }
}

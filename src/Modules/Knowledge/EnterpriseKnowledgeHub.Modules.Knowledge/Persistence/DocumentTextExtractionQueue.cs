using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

internal sealed class DocumentTextExtractionQueue(
    KnowledgeDbContext db,
    TimeProvider clock,
    IOptions<DocumentTextExtractionOptions> extractionOptions,
    IOptions<DocumentTextExtractionWorkerOptions> options) : IDocumentTextExtractionQueue
{
    public async Task<DocumentTextExtraction?> ClaimAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var enabledFormats = DocumentFormat.All.Where(extractionOptions.Value.IsEnabled).Select(format => format.Id).ToArray();

        var eligible = db.DocumentTextExtractions.Where(item => enabledFormats.Contains(item.FormatId) &&
            ((item.Status == TextExtractionStatus.Pending && item.NextAttemptAt <= now) ||
             (item.Status == TextExtractionStatus.Processing && item.LeaseExpiresAt <= now)));

        var candidates = await eligible
            .OrderBy(item => item.NextAttemptAt)
            .Select(item => item.Id)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var id in candidates)
        {
            var owner = Guid.NewGuid();
            var updated = await eligible.Where(item => item.Id == id).ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, TextExtractionStatus.Processing)
                .SetProperty(item => item.LeaseOwner, owner)
                .SetProperty(item => item.LeaseExpiresAt, now.AddSeconds(options.Value.LeaseSeconds))
                .SetProperty(item => item.Attempts, item => item.Attempts + 1), cancellationToken);

            if (updated == 1)
            {
                return await db.DocumentTextExtractions
                    .AsNoTracking()
                    .Where(item => item.Id == id && item.LeaseOwner == owner)
                    .Select(item => new DocumentTextExtraction
                    {
                        Id = item.Id, 
                        OrganizationId = item.OrganizationId, 
                        DocumentId = item.DocumentId,
                        SourceETag = item.SourceETag, 
                        FormatId = item.FormatId, 
                        ExtractorVersion = item.ExtractorVersion,
                        Status = item.Status, 
                        Attempts = item.Attempts, 
                        LeaseOwner = item.LeaseOwner,
                        LeaseExpiresAt = item.LeaseExpiresAt
                    }).SingleAsync(cancellationToken);
            }
        }

        return null;
    }

    public Task<int> CompleteAsync(DocumentTextExtraction work, string text, CancellationToken cancellationToken) =>
        Owned(work).Where(item => db.Documents.Any(document =>
            document.Id == item.DocumentId && document.OrganizationId == item.OrganizationId &&
            document.Status == DocumentStatus.Uploaded && document.ValidatedETag == item.SourceETag))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Text, text)
            .SetProperty(item => item.CharacterCount, text.Length)
            .SetProperty(item => item.Status, TextExtractionStatus.Completed)
            .SetProperty(item => item.CompletedAt, clock.GetUtcNow().UtcDateTime)
            .SetProperty(item => item.LastError, (string?)null)
            .SetProperty(item => item.LeaseOwner, (Guid?)null)
            .SetProperty(item => item.LeaseExpiresAt, (DateTime?)null), cancellationToken);

    public Task<int> FailAsync(DocumentTextExtraction work, string errorCode, CancellationToken cancellationToken) =>
        Owned(work).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, TextExtractionStatus.Failed)
            .SetProperty(item => item.LastError, errorCode)
            .SetProperty(item => item.CompletedAt, clock.GetUtcNow().UtcDateTime)
            .SetProperty(item => item.LeaseOwner, (Guid?)null)
            .SetProperty(item => item.LeaseExpiresAt, (DateTime?)null), cancellationToken);

    public Task<int> RetryAsync(DocumentTextExtraction work, string errorCode, CancellationToken cancellationToken)
    {
        var exhausted = work.Attempts >= options.Value.MaxAttempts;
        var delay = Math.Min(options.Value.RetryMaxSeconds,
            options.Value.RetryBaseSeconds * Math.Pow(2, Math.Min(work.Attempts - 1, 20)));
            
        return Owned(work).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, exhausted ? TextExtractionStatus.DeadLetter : TextExtractionStatus.Pending)
            .SetProperty(item => item.NextAttemptAt, clock.GetUtcNow().UtcDateTime.AddSeconds(delay))
            .SetProperty(item => item.CompletedAt, exhausted ? clock.GetUtcNow().UtcDateTime : (DateTime?)null)
            .SetProperty(item => item.LastError, errorCode)
            .SetProperty(item => item.LeaseOwner, (Guid?)null)
            .SetProperty(item => item.LeaseExpiresAt, (DateTime?)null), cancellationToken);
    }

    private IQueryable<DocumentTextExtraction> Owned(DocumentTextExtraction work)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        return db.DocumentTextExtractions.Where(item => 
            item.Id == work.Id 
            && item.OrganizationId == work.OrganizationId 
            && item.DocumentId == work.DocumentId 
            && item.SourceETag == work.SourceETag 
            && item.Status == TextExtractionStatus.Processing 
            && item.LeaseOwner == work.LeaseOwner 
            && item.LeaseExpiresAt > now);
    }
}

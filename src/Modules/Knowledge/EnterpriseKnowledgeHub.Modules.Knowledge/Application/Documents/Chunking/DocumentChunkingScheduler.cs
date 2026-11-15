using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

internal sealed class DocumentChunkingScheduler(
    KnowledgeDbContext db,
    DocumentChunkerRegistry chunkers,
    IOptions<DocumentChunkingOptions> options,
    IOptions<DocumentChunkingWorkerOptions> workerOptions,
    TimeProvider clock) : IDocumentChunkingScheduler
{
    public async Task ScheduleAsync(CancellationToken cancellationToken)
    {
        foreach (var format in DocumentFormat.All.Where(options.Value.IsEnabled))
        {
            var chunker = chunkers.Find(format) ?? throw new InvalidOperationException("An enabled format requires a registered chunker.");

            var settings = options.Value.Settings;
            var key = chunkers.ConfigurationKey(format, settings)!;
            
            // Bounded maintenance reads metadata only. Ownership comes from persisted sources.
            var sources = await db.DocumentTextExtractions.AsNoTracking()
                .Where(extraction => extraction.FormatId == format.Id && extraction.Status == TextExtractionStatus.Completed)
                .Where(extraction => db.Documents.Any(document => document.Id == extraction.DocumentId &&
                    document.OrganizationId == extraction.OrganizationId && document.Status == DocumentStatus.Uploaded &&
                    document.ValidatedETag == extraction.SourceETag))
                .Where(extraction => !db.DocumentChunkings.Any(chunking => chunking.ExtractionId == extraction.Id && chunking.ConfigurationKey == key))
                .OrderBy(extraction => extraction.CompletedAt)
                .Select(extraction => new { extraction.Id, extraction.OrganizationId, extraction.DocumentId, extraction.SourceETag })
                .Take(workerOptions.Value.BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var source in sources)
            {
                var now = clock.GetUtcNow().UtcDateTime;
                var work = new DocumentChunking
                {
                    OrganizationId = source.OrganizationId, 
                    DocumentId = source.DocumentId, 
                    ExtractionId = source.Id,
                    SourceETag = source.SourceETag, 
                    FormatId = format.Id, 
                    ChunkerVersion = chunker.Version, 
                    ConfigurationKey = key,
                    TargetTokens = settings.TargetTokens, 
                    MaxTokens = settings.MaxTokens, 
                    OverlapTokens = settings.OverlapTokens,
                    MaxChunksPerDocument = settings.MaxChunksPerDocument, 
                    MaxSourceCharacters = settings.MaxSourceCharacters,
                    CreatedAt = now, 
                    NextAttemptAt = now,
                    Status = DocumentChunkingStatus.Pending
                };

                db.DocumentChunkings.Add(work);
                
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    db.Entry(work).State = EntityState.Detached;
                    if (!await db.DocumentChunkings.AnyAsync(item => item.ExtractionId == source.Id && item.ConfigurationKey == key, cancellationToken))
                    {
                        throw;
                    }
                }
            }
        }
    }
}

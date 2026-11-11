using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

internal sealed class DocumentTextExtractionScheduler(
    KnowledgeDbContext db,
    DocumentTextExtractorRegistry extractors,
    IOptions<DocumentTextExtractionOptions> options,
    IOptions<DocumentTextExtractionWorkerOptions> workerOptions,
    TimeProvider clock) : IDocumentTextExtractionScheduler
{
    public DocumentTextExtraction? Stage(Document document)
    {
        var format = DocumentFormat.FromFileName(document.BlobReference);
        if (!DocumentProcessingGate.CanStartExtraction(document) || format is null || !options.Value.IsEnabled(format))
        {
            return null;
        }

        var extractor = extractors.Find(format)
            ?? throw new InvalidOperationException("An enabled format requires an explicitly registered extractor.");
            
        var now = clock.GetUtcNow().UtcDateTime;
        var item = new DocumentTextExtraction
        {
            OrganizationId = document.OrganizationId,
            DocumentId = document.Id,
            SourceETag = document.ValidatedETag!,
            FormatId = format.Id,
            ExtractorVersion = extractor.Version,
            CreatedAt = now,
            NextAttemptAt = now
        };

        db.DocumentTextExtractions.Add(item);
        
        return item;
    }

    public async Task ScheduleAsync(CancellationToken cancellationToken)
    {
        var extensions = DocumentFormat.All.Where(options.Value.IsEnabled).Select(format => format.Extension).ToArray();

        // System maintenance reads only bounded reservation metadata. Ownership of each job
        // always comes from its persisted Document, never an ambient user or client input.
        var candidates = await db.Documents.AsNoTracking()
            .Where(document => document.Status == DocumentStatus.Uploaded &&
                !string.IsNullOrEmpty(document.ValidatedETag) &&
                extensions.Any(extension => document.BlobReference.EndsWith(extension)))
            .Where(document => !db.DocumentTextExtractions.Any(extraction =>
                extraction.OrganizationId == document.OrganizationId &&
                extraction.DocumentId == document.Id &&
                extraction.SourceETag == document.ValidatedETag))
            .OrderBy(document => document.CreatedAt)
            .Take(workerOptions.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var document in candidates)
        {
            var item = Stage(document);
            if (item is null)
            {
                continue;
            }
            
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.Entry(item).State = EntityState.Detached;
                // A competing scheduler/finalizer may have inserted the same source identity.
                if (!await db.DocumentTextExtractions.AnyAsync(extraction =>
                    extraction.OrganizationId == document.OrganizationId &&
                    extraction.DocumentId == document.Id &&
                    extraction.SourceETag == document.ValidatedETag, cancellationToken))
                {
                    throw;
                }
            }
        }
    }
}

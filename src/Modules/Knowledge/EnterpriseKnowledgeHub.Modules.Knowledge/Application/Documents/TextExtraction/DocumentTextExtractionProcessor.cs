using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

internal sealed class DocumentTextExtractionProcessor(
    KnowledgeDbContext db,
    IDocumentBlobStorage storage,
    IDocumentTextExtractionQueue queue,
    DocumentTextExtractorRegistry extractors,
    IOptions<DocumentTextExtractionOptions> options) : IDocumentTextExtractionProcessor
{
    public async Task ProcessAsync(DocumentTextExtraction work, CancellationToken cancellationToken)
    {
        // A persisted work item identifies its Organization; never use an ambient request context.
        var document = await db.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(document =>
                document.Id == work.DocumentId && document.OrganizationId == work.OrganizationId, cancellationToken);

        if (document is null || !DocumentProcessingGate.CanStartExtraction(document) || document.ValidatedETag != work.SourceETag)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.SourceNotEligible);
        }

        var format = DocumentFormat.FromFileName(document.BlobReference);
        var canonicalReference = $"organizations/{document.OrganizationId:N}/documents/{document.Id:N}{format?.Extension}";
        if (format is null || format.Id != work.FormatId || document.BlobReference != canonicalReference || document.ContentType != format.ContentType)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.SourceIdentityMismatch);
        }

        if (!options.Value.IsEnabled(format))
        {
            // Configuration is fixed for the lifetime of this process. ClaimAsync excludes
            // disabled formats; this also protects direct/internal invocations before any I/O.
            return;
        }

        var extractor = extractors.Find(format);
        if (extractor is null || extractor.Version != work.ExtractorVersion)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.ExtractorVersionUnavailable);
        }

        var source = await storage.OpenVerifiedReadAsync(document.BlobReference, work.SourceETag,
            options.Value.MaxSourceSizeBytes, cancellationToken);

        await using var content = source.Content;
        var text = await extractor.ExtractAsync(content, source.ContentLength, options.Value.MaxTextCharacters, cancellationToken);
        if (text.Length > options.Value.MaxTextCharacters)
        {
            throw new DocumentTextExtractionException(DocumentTextExtractionErrorCodes.TextLimitExceeded);
        }

        if (await queue.CompleteAsync(work, text, cancellationToken) == 0)
        {
            // The conditional completion fences expired owners and changed source state.
            // If the lease still belongs to us, record the source-state failure; otherwise no-op.
            await queue.FailAsync(work, DocumentTextExtractionErrorCodes.SourceChangedDuringExtraction, cancellationToken);
        }
    }
}

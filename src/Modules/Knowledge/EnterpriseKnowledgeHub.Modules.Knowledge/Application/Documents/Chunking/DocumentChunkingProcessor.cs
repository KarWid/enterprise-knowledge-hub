using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

internal sealed class DocumentChunkingProcessor(
    KnowledgeDbContext db, 
    IDocumentChunkingQueue queue, 
    DocumentChunkerRegistry chunkers,
    IOptions<DocumentChunkingOptions> options) : IDocumentChunkingProcessor
{
    public async Task ProcessAsync(DocumentChunking work, CancellationToken cancellationToken)
    {
        var format = DocumentFormat.FromId(work.FormatId);
        if (format is null)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.SourceNotEligible);
        }

        if (!options.Value.IsEnabled(format))
        {
            return;
        }

        var chunker = chunkers.Find(format);
        if (chunker is null || chunker.Version != work.ChunkerVersion)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.ChunkerVersionUnavailable);
        }

        var settings = new DocumentChunkingSettings(
            work.TargetTokens, 
            work.MaxTokens, 
            work.OverlapTokens,
            work.MaxChunksPerDocument,
             work.MaxSourceCharacters);

        if (!settings.IsValid() || chunkers.ConfigurationKey(format, settings) != work.ConfigurationKey)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.ChunkerVersionUnavailable);
        }

        var document = await db.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == work.DocumentId && item.OrganizationId == work.OrganizationId, cancellationToken);

        if (document is null 
            || !DocumentProcessingGate.CanStartExtraction(document) 
            || document.ValidatedETag != work.SourceETag 
            || DocumentFormat.FromFileName(document.Name) != format)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.SourceNotEligible);
        }

        var sources = db.DocumentTextExtractions.AsNoTracking().Where(extraction => extraction.Id == work.ExtractionId &&
            extraction.OrganizationId == work.OrganizationId && extraction.DocumentId == work.DocumentId &&
            extraction.SourceETag == work.SourceETag && extraction.FormatId == work.FormatId &&
            extraction.Status == TextExtractionStatus.Completed);
            
        var sourceLength = await sources.Select(extraction => extraction.CharacterCount).SingleOrDefaultAsync(cancellationToken);
        if (sourceLength is null)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.SourceNotEligible);
        }

        if (sourceLength > settings.MaxSourceCharacters)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.SourceLimitExceeded);
        }

        var text = await sources.Select(extraction => extraction.Text).SingleOrDefaultAsync(cancellationToken);
        if (text is null)
        {
            throw new DocumentChunkingException(DocumentChunkingErrorCodes.SourceNotEligible);
        }

        var chunks = chunker.Chunk(text, document.Name, settings, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (await queue.CompleteAsync(work, chunks, cancellationToken) == 0)
        {
            await queue.FailAsync(work, DocumentChunkingErrorCodes.SourceNotEligible, cancellationToken);
        }
    }
}

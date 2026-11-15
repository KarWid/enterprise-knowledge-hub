using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

internal sealed record DocumentChunkingResult(string Status, int? ChunkCount = null, DateTime? CompletedAt = null, string? FailureCode = null);

internal sealed class DocumentChunkingStatusResolver(
    DocumentChunkerRegistry chunkers,
    IOptions<DocumentChunkingOptions> options,
    IOptions<DocumentChunkingWorkerOptions> workerOptions)
{
    public string[] CurrentConfigurationKeys() => DocumentFormat.All
        .Select(format => chunkers.ConfigurationKey(format, options.Value.Settings))
        .OfType<string>()
        .ToArray();

    public DocumentChunkingResult Resolve(
        DocumentStatus documentStatus,
        string documentName,
        string? validatedETag,
        TextExtractionStatus? extractionStatus,
        DocumentChunkingStatusProjection? chunking)
    {
        if (documentStatus != DocumentStatus.Uploaded || string.IsNullOrEmpty(validatedETag))
        {
            return new("NotAvailable");
        }

        var format = DocumentFormat.FromFileName(documentName);
        var formatEnabled = format is not null && options.Value.IsEnabled(format);
        var status = ResolveStatus(formatEnabled, extractionStatus, chunking?.Status);

        return new DocumentChunkingResult(
            status, 
            chunking?.ChunkCount,
            chunking?.Status == DocumentChunkingStatus.Completed ? chunking.CompletedAt : null, 
            chunking?.LastError);
    }

    private string ResolveStatus(
        bool formatEnabled, 
        TextExtractionStatus? extractionStatus,
        DocumentChunkingStatus? chunkingStatus)
    {
        var isAwaitingCompletion = chunkingStatus is null or DocumentChunkingStatus.Pending or DocumentChunkingStatus.Processing;
        if (!isAwaitingCompletion)
        {
            return chunkingStatus!.Value.ToString();
        }

        if (!formatEnabled && chunkingStatus is null)
        {
            return "Disabled";
        }

        if (!formatEnabled || !workerOptions.Value.Enabled)
        {
            return "Paused";
        }

        if (extractionStatus != TextExtractionStatus.Completed)
        {
            return "NotAvailable";
        }

        return (chunkingStatus ?? DocumentChunkingStatus.Pending).ToString();
    }
}

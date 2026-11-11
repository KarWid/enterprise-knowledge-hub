using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

internal sealed class DocumentTextExtractionStatusResolver(
    IOptions<DocumentTextExtractionOptions> options,
    IOptions<DocumentTextExtractionWorkerOptions> workerOptions)
{
    public DocumentTextExtractionResult Resolve(
        DocumentStatus documentStatus,
        string documentName,
        string? validatedETag,
        DocumentTextExtractionStatusProjection? extraction)
    {
        if (documentStatus != DocumentStatus.Uploaded || string.IsNullOrEmpty(validatedETag))
        {
            return new("NotAvailable");
        }

        var format = DocumentFormat.FromFileName(documentName);
        var enabled = format is not null && options.Value.IsEnabled(format);
        var status = extraction?.Status.ToString() ?? "Pending";

        if (extraction?.Status is null or TextExtractionStatus.Pending or TextExtractionStatus.Processing)
        {
            status = !enabled
                ? extraction is null ? "Disabled" : "Paused"
                : !workerOptions.Value.Enabled ? "Paused" : status;
        }

        return new(
            status,
            extraction?.CharacterCount,
            extraction?.Status == TextExtractionStatus.Completed ? extraction.CompletedAt : null,
            extraction?.LastError);
    }
}

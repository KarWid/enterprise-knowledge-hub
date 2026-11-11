using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public static class DocumentProcessingGate
{
    public static bool HasVerifiedUpload(Document document) =>
        document.Status == DocumentStatus.Uploaded && !string.IsNullOrEmpty(document.ValidatedETag);

    // Only explicitly registered/enabled extractors may consume verified bytes. The
    // current Markdown extractor only decodes text; verification is not malware clearance.
    // Any future required scan must add durable clean evidence for this exact ETag here.
    public static bool CanStartExtraction(Document document) => HasVerifiedUpload(document);
}

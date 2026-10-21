using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public static class DocumentProcessingGate
{
    public static bool HasVerifiedUpload(Document document) =>
        document.Status == DocumentStatus.Uploaded && !string.IsNullOrEmpty(document.ValidatedETag);

    // Extraction/indexing is not enabled in this milestone. Replace this gate only when
    // every required check has durable evidence tied to document.ValidatedETag.
    // In particular, a PDF header is never evidence of a clean malware scan.
    public static bool CanStartExtraction(Document document) => false;
}

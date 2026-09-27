namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

// TODO @KWidla: think about extending this record with additional maximum upload size or other constraints
internal sealed record DocumentUploadSessionResult(
    DocumentResult Document,
    Uri UploadUri,
    DateTime ExpiresAt);

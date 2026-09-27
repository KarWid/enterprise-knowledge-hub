namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentUploadSessionResponse(
    Guid DocumentId,
    Uri UploadUri,
    DateTime ExpiresAt);

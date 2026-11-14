using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentUploadSessionResponse(
    [property: Required] Guid DocumentId,
    [property: Required] Uri UploadUri,
    [property: Required] DateTime ExpiresAt,
    [property: Required] string ContentType);

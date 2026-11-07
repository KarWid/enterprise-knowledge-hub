namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentUploadCapabilitiesResponse(IReadOnlyList<DocumentUploadFormatResponse> Formats, long MaxSizeBytes);

public sealed record DocumentUploadFormatResponse(string Id, string Extension, string ContentType);

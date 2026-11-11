using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentUploadCapabilitiesResponse(
    [property: Required] IReadOnlyList<DocumentUploadFormatResponse> Formats,
    [property: Required] long MaxSizeBytes);

public sealed record DocumentUploadFormatResponse(
    [property: Required] string Id,
    [property: Required] string Extension,
    [property: Required] string ContentType);

using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentResponse(
    [property: Required] Guid Id,
    [property: Required] string Name,
    [property: Required] string ContentType,
    [property: Required] string Status,
    [property: Required] DateTime CreatedAt,
    DocumentTextExtractionResponse? TextExtraction = null);

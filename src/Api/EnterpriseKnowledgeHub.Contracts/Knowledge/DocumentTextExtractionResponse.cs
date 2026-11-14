using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentTextExtractionResponse([property: Required] string Status, int? CharacterCount, DateTime? CompletedAt, string? FailureCode);

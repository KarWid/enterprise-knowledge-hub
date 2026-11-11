namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

internal sealed record DocumentTextExtractionResult(string Status, int? CharacterCount = null, DateTime? CompletedAt = null, string? FailureCode = null);

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentTextPreview;

internal sealed record DocumentTextPreviewResult(string Text, int CharacterCount, bool IsTruncated, DateTime CompletedAt);

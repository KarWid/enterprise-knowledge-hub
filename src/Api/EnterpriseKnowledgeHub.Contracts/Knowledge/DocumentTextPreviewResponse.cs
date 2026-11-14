using System.ComponentModel.DataAnnotations;

namespace EnterpriseKnowledgeHub.Contracts.Knowledge;

public sealed record DocumentTextPreviewResponse(
    [property: Required] string Text,
    [property: Required] int CharacterCount,
    [property: Required] bool IsTruncated,
    [property: Required] DateTime CompletedAt);

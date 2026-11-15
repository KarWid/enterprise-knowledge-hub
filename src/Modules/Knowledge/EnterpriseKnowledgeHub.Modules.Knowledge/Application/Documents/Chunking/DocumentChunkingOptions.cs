using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public sealed class DocumentChunkingOptions
{
    public const string SectionName = "DocumentChunking";
    public int TargetTokens { get; init; } = 500;
    public int MaxTokens { get; init; } = 800;
    public int OverlapTokens { get; init; } = 64;
    public int MaxChunksPerDocument { get; init; } = 10_000;
    public int MaxSourceCharacters { get; init; } = 5_000_000;
    public Dictionary<string, bool> EnabledFormats { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [DocumentFormat.Markdown.Id] = true,
        [DocumentFormat.Pdf.Id] = false
    };

    public bool IsEnabled(DocumentFormat format) => EnabledFormats.TryGetValue(format.Id, out var enabled) && enabled;

    public DocumentChunkingSettings Settings => new(TargetTokens, MaxTokens, OverlapTokens, MaxChunksPerDocument, MaxSourceCharacters);

    public bool IsValid() => Settings.IsValid() 
        && EnabledFormats is not null 
        && EnabledFormats.Keys.All(id => DocumentFormat.FromId(id) is not null);
}

public sealed record DocumentChunkingSettings(
    int TargetTokens, 
    int MaxTokens, 
    int OverlapTokens, 
    int MaxChunksPerDocument, 
    int MaxSourceCharacters)
{
    public bool IsValid() => 
        TargetTokens >= 32 && MaxTokens >= 64 
        && TargetTokens <= MaxTokens && MaxTokens <= 8192 &&
        OverlapTokens >= 0 && OverlapTokens < TargetTokens / 2 &&
        MaxChunksPerDocument is > 0 and <= 100_000 
        && MaxSourceCharacters is > 0 and <= 50_000_000;
}

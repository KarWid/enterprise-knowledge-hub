using System.Security.Cryptography;
using System.Text;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public sealed class DocumentChunkerRegistry(IEnumerable<IDocumentChunker> chunkers)
{
    private readonly IReadOnlyDictionary<string, IDocumentChunker> byFormat = chunkers
        .ToDictionary(chunker => chunker.Format.Id, StringComparer.OrdinalIgnoreCase);

    public IDocumentChunker? Find(DocumentFormat format) => byFormat.GetValueOrDefault(format.Id);
    public bool SupportsEnabledFormats(DocumentChunkingOptions options) => options.IsValid() &&
        DocumentFormat.All.Where(options.IsEnabled).All(format =>
            Find(format) is { Version.Length: > 0 and <= 64 } chunker && !string.IsNullOrWhiteSpace(chunker.Version));

    public string? ConfigurationKey(DocumentFormat format, DocumentChunkingSettings settings)
    {
        var chunker = Find(format);
        if (chunker is null)
        {
            return null;   
        }
        
        // Include all effective settings. Changes produce a separate, immutable chunk set.
        var identity = FormattableString.Invariant($"{format.Id}|{chunker.Version}|{settings.TargetTokens}|{settings.MaxTokens}|{settings.OverlapTokens}|{settings.MaxChunksPerDocument}|{settings.MaxSourceCharacters}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }
}

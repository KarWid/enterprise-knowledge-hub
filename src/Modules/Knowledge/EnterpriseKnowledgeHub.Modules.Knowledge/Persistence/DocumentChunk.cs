namespace EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;

public sealed class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ChunkingId { get; set; }
    public int Ordinal { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? HeadingPath { get; set; }
    public int TokenCount { get; set; }
}

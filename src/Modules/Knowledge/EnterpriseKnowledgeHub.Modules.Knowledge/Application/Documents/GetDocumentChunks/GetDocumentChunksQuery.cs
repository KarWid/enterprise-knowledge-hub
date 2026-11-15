using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

internal sealed record GetDocumentChunksQuery : IRequest<DocumentChunksResult?>
{
    public const int DefaultLimit = 5;
    public const int MaximumLimit = 20;

    public Guid DocumentId { get; }
    public int Offset { get; }
    public int Limit { get; }

    public GetDocumentChunksQuery(Guid documentId, int offset = 0, int limit = DefaultLimit)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be zero or greater.");
        }

        if (limit is < 1 or > MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, $"Limit must be between 1 and {MaximumLimit}.");
        }

        DocumentId = documentId;
        Offset = offset;
        Limit = limit;
    }
}

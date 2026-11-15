using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentChunks;

internal sealed class GetDocumentChunksQueryHandler(
    KnowledgeDbContext db,
    IOrganizationAccessService access,
    DocumentChunkingStatusResolver status,
    IDocumentDiagnosticsAccessService diagnosticsAccess)
    : IRequestHandler<GetDocumentChunksQuery, DocumentChunksResult?>
{
    public async ValueTask<DocumentChunksResult?> Handle(GetDocumentChunksQuery request, CancellationToken cancellationToken)
    {
        var membership = await access.GetCurrentOrganizationMembershipAsync(cancellationToken);
        if (!await diagnosticsAccess.CanViewDiagnosticsAsync(cancellationToken))
        {
            return null;
        }

        var eligibleChunkings = GetEligibleChunkings(membership.OrganizationId, request.DocumentId);
        var source = await eligibleChunkings
            .Select(chunking => new { chunking.Id, chunking.ChunkCount })
            .SingleOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            return null;
        }

        var previews = await db.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.ChunkingId == source.Id
                && chunk.OrganizationId == membership.OrganizationId
                && chunk.DocumentId == request.DocumentId)
            // Publication is atomic, but document status/source eligibility could change between these two reads.
            .Where(chunk => eligibleChunkings.Any(chunking => chunking.Id == chunk.ChunkingId))
            .OrderBy(chunk => chunk.Ordinal)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(chunk => new ChunkPreviewProjection(
                chunk.Id,
                chunk.Ordinal,
                // Bound the text in SQL; the extra character detects truncation.
                chunk.Content.Substring(0, DocumentChunkPreviewMapper.MaximumPreviewCharacters + 1),
                chunk.HeadingPath,
                chunk.TokenCount))
            .ToListAsync(cancellationToken);

        var items = previews.Select(DocumentChunkPreviewMapper.MapToPreview).ToList();
        var chunkCount = source.ChunkCount!.Value;
        var hasMore = (long)request.Offset + items.Count < chunkCount;

        return new DocumentChunksResult(items, chunkCount, request.Offset, hasMore);
    }

    private IQueryable<DocumentChunking> GetEligibleChunkings(Guid organizationId, Guid documentId)
    {
        var configurationKeys = status.CurrentConfigurationKeys();

        return db.DocumentChunkings
            .AsNoTracking()
            .Where(chunking => chunking.OrganizationId == organizationId && chunking.DocumentId == documentId)
            .Where(chunking => chunking.Status == DocumentChunkingStatus.Completed
                && configurationKeys.Contains(chunking.ConfigurationKey))
            // Completion already validates extraction success; only current document eligibility can change here.
            .Where(chunking => db.Documents.Any(document =>
                document.Id == chunking.DocumentId
                && document.OrganizationId == chunking.OrganizationId
                && document.Status == DocumentStatus.Uploaded
                && document.ValidatedETag == chunking.SourceETag));
    }

}

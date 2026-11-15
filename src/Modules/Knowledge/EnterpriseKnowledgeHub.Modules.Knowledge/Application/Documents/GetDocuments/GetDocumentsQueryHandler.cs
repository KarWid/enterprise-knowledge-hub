using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;

internal sealed class GetDocumentsQueryHandler(
    KnowledgeDbContext db,
    IOrganizationAccessService organizationAccessService,
    DocumentTextExtractionStatusResolver extractionStatusResolver,
    DocumentChunkingStatusResolver chunkingStatusResolver)
    : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentResult>>
{
    // TODO @KWidla: consider adding filtering, paging, or other query optimizations
    public async ValueTask<IReadOnlyList<DocumentResult>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await organizationAccessService
            .GetCurrentOrganizationMembershipAsync(cancellationToken);

        // TODO: Replace this development placeholder with backend role/permission authorization.
        var hasPermission = true;
        var documentsQuery = db.Documents
            .AsNoTracking()
            .Where(document => document.OrganizationId == membership.OrganizationId)
            .OrderByDescending(document => document.CreatedAt);

        IQueryable<DocumentResultProjection> projection;
        if (hasPermission)
        {
            var configurationKeys = chunkingStatusResolver.CurrentConfigurationKeys();
            projection = documentsQuery.SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking(),
                db.DocumentChunkings.AsNoTracking().Where(chunking => configurationKeys.Contains(chunking.ConfigurationKey)));
        }
        else
        {
            projection = documentsQuery.SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking());
        }

        var documents = await projection.ToListAsync(cancellationToken);

        return documents.Select(document => hasPermission
            ? document.ToResult(extractionStatusResolver, chunkingStatusResolver)
            : document.ToResult(extractionStatusResolver)).ToList();
    }
}

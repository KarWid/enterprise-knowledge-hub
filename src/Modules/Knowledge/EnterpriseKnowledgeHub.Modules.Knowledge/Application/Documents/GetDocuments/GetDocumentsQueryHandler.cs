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
    DocumentChunkingStatusResolver chunkingStatusResolver,
    IDocumentDiagnosticsAccessService diagnosticsAccess)
    : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentResult>>
{
    // TODO @KWidla: consider adding filtering, paging, or other query optimizations
    public async ValueTask<IReadOnlyList<DocumentResult>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await organizationAccessService
            .GetCurrentOrganizationMembershipAsync(cancellationToken);

        var includeDiagnostics = await diagnosticsAccess.CanViewDiagnosticsAsync(cancellationToken);
        var documentsQuery = db.Documents
            .AsNoTracking()
            .Where(document => document.OrganizationId == membership.OrganizationId)
            .OrderByDescending(document => document.CreatedAt);

        IQueryable<DocumentResultProjection> projection;
        if (includeDiagnostics)
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

        return documents.Select(document => includeDiagnostics
            ? document.ToResult(extractionStatusResolver, chunkingStatusResolver)
            : document.ToResult(extractionStatusResolver)).ToList();
    }
}

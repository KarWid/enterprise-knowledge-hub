using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;

internal sealed class GetDocumentsQueryHandler(
    KnowledgeDbContext db,
    IOrganizationAccessService organizationAccessService,
    DocumentTextExtractionStatusResolver extractionStatus)
    : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentResult>>
{
    // TODO @KWidla: consider adding filtering, paging, or other query optimizations
    public async ValueTask<IReadOnlyList<DocumentResult>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await organizationAccessService
            .GetCurrentOrganizationMembershipAsync(cancellationToken);

        var documents = await db.Documents
            .AsNoTracking()
            .Where(document => document.OrganizationId == membership.OrganizationId)
            .OrderByDescending(document => document.CreatedAt)
            .SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking())
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToResult(extractionStatus)).ToList();
    }
}

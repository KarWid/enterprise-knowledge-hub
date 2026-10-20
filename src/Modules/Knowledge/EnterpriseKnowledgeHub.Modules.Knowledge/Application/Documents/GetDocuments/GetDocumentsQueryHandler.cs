using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;

internal sealed class GetDocumentsQueryHandler(
    KnowledgeDbContext db,
    IOrganizationAccessService organizationAccessService)
    : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentResult>>
{
    // TODO @KWidla: consider adding filtering, paging, or other query optimizations
    public async ValueTask<IReadOnlyList<DocumentResult>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await organizationAccessService
            .GetCurrentOrganizationMembershipAsync(cancellationToken);

        return await db.Documents
            .AsNoTracking()
            .Where(document => document.OrganizationId == membership.OrganizationId)
            .OrderByDescending(document => document.CreatedAt)
            .Select(document => new DocumentResult(
                document.Id,
                document.Name,
                document.ContentType,
                document.Status,
                document.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

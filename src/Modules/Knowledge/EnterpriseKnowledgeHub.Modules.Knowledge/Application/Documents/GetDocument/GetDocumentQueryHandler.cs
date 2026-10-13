using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(KnowledgeDbContext db, IOrganizationAccessService access)
    : IRequestHandler<GetDocumentQuery, DocumentResult?>
{
    public async ValueTask<DocumentResult?> Handle(GetDocumentQuery request, CancellationToken ct)
    {
        var membership = await access.GetCurrentOrganizationMembershipAsync(ct);
        return await db.Documents.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.OrganizationId == membership.OrganizationId)
            .Select(d => new DocumentResult(d.Id, d.Name, d.ContentType, d.Status, d.CreatedAt))
            .SingleOrDefaultAsync(ct);
    }
}

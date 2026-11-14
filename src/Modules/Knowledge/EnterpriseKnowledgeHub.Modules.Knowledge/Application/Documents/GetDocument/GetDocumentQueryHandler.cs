using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(KnowledgeDbContext db, IOrganizationAccessService access, DocumentTextExtractionStatusResolver extractionStatus)
    : IRequestHandler<GetDocumentQuery, DocumentResult?>
{
    public async ValueTask<DocumentResult?> Handle(GetDocumentQuery request, CancellationToken ct)
    {
        var membership = await access.GetCurrentOrganizationMembershipAsync(ct);
        var document = await db.Documents.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.OrganizationId == membership.OrganizationId)
            .SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking())
            .SingleOrDefaultAsync(ct);

        return document?.ToResult(extractionStatus);
    }
}

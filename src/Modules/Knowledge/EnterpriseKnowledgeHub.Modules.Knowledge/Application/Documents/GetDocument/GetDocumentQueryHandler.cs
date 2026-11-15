using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;

internal sealed class GetDocumentQueryHandler(
    KnowledgeDbContext db, 
    IOrganizationAccessService access,
    DocumentTextExtractionStatusResolver extractionStatus,
    DocumentChunkingStatusResolver chunkingStatus,
    IDocumentDiagnosticsAccessService diagnosticsAccess)
    : IRequestHandler<GetDocumentQuery, DocumentResult?>
{
    public async ValueTask<DocumentResult?> Handle(GetDocumentQuery request, CancellationToken ct)
    {
        var membership = await access.GetCurrentOrganizationMembershipAsync(ct);
        var includeDiagnostics = await diagnosticsAccess.CanViewDiagnosticsAsync(ct);
        var documentsQuery = db.Documents.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.OrganizationId == membership.OrganizationId);

        IQueryable<DocumentResultProjection> projection;
        if (includeDiagnostics)
        {
            var configurationKeys = chunkingStatus.CurrentConfigurationKeys();
            projection = documentsQuery.SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking(),
                db.DocumentChunkings.AsNoTracking().Where(chunking => configurationKeys.Contains(chunking.ConfigurationKey)));
        }
        else
        {
            projection = documentsQuery.SelectDocumentResults(db.DocumentTextExtractions.AsNoTracking());
        }

        var document = await projection.SingleOrDefaultAsync(ct);

        if (document is null)
        {
            return null;
        }

        return includeDiagnostics
            ? document.ToResult(extractionStatus, chunkingStatus)
            : document.ToResult(extractionStatus);
    }
}

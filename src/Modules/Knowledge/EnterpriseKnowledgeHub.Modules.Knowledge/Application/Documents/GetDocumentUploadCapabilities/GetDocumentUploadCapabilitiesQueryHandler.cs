using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Mediator;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentUploadCapabilities;

internal sealed class GetDocumentUploadCapabilitiesQueryHandler(
    IOrganizationAccessService organizationAccessService,
    IOptions<DocumentUploadOptions> options)
    : IRequestHandler<GetDocumentUploadCapabilitiesQuery, DocumentUploadCapabilitiesResult>
{
    public async ValueTask<DocumentUploadCapabilitiesResult> Handle(
        GetDocumentUploadCapabilitiesQuery request, CancellationToken cancellationToken)
    {
        await organizationAccessService.GetCurrentOrganizationMembershipAsync(cancellationToken);

        var settings = options.Value;
        var enabledFormatsForNewUploads = DocumentFormat.All
            .Where(settings.IsEnabled)
            .Select(format => new DocumentUploadFormatResult(
                format.Id,
                format.Extension,
                format.ContentType))
            .ToArray();

        return new DocumentUploadCapabilitiesResult(
            enabledFormatsForNewUploads,
            settings.MaxUploadSizeBytes);
    }
}

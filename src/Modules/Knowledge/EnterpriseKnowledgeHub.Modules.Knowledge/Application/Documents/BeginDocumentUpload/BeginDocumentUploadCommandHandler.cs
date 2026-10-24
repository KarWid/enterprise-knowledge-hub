using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.BeginDocumentUpload;

internal sealed class BeginDocumentUploadCommandHandler(
    KnowledgeDbContext db,
    IOrganizationAccessService organizationAccessService,
    IDocumentBlobStorage blobStorage,
    IOptions<DocumentUploadOptions> uploadOptions,
    ILogger<BeginDocumentUploadCommandHandler> logger)
    : IRequestHandler<BeginDocumentUploadCommand, DocumentUploadSessionResult>
{
    private const int MaximumUploadSasLifetimeMinutes = 15;

    public async ValueTask<DocumentUploadSessionResult> Handle(
        BeginDocumentUploadCommand request,
        CancellationToken cancellationToken)
    {
        var membership = await organizationAccessService
            .GetCurrentOrganizationMembershipAsync(cancellationToken);

        var expiresAt = GetUploadExpiration(uploadOptions.Value);

        var document = Document.CreatePendingUpload(
            membership.OrganizationId,
            Path.GetFileName(request.FileName ?? string.Empty),
            membership.UserId,
            expiresAt);
            
        var uploadUri = await blobStorage.CreateUploadUriAsync(
            document.BlobReference,
            new DateTimeOffset(expiresAt),
            cancellationToken);

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Document upload {DocumentId} was reserved for organization {OrganizationId}.",
            document.Id,
            document.OrganizationId);

        return new DocumentUploadSessionResult(
            new DocumentResult(
                document.Id,
                document.Name,
                document.ContentType,
                document.Status,
                document.CreatedAt),
            uploadUri,
            expiresAt);
    }

    private static DateTime GetUploadExpiration(DocumentUploadOptions options)
    {
        if (options.UploadSasLifetimeMinutes is <= 0 or > MaximumUploadSasLifetimeMinutes)
        {
            throw new InvalidOperationException(
                $"Documents:UploadSasLifetimeMinutes must be greater than zero and no more than {MaximumUploadSasLifetimeMinutes}.");
        }

        return DateTime.UtcNow.AddMinutes(options.UploadSasLifetimeMinutes);
    }
}

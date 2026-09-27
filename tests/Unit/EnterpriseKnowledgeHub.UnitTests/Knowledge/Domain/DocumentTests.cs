using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Domain;

public sealed class DocumentTests
{
    [Fact]
    public void CreatePendingUpload_WithValidPdf_CreatesPendingDocumentBoundToOrganization()
    {
        var organizationId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var document = Document.CreatePendingUpload(organizationId, "  handbook.PDF  ", createdBy, expiresAt);

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal(organizationId, document.OrganizationId);
        Assert.Equal(createdBy, document.CreatedBy);
        Assert.Equal("handbook.PDF", document.Name);
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.Equal(expiresAt, document.UploadExpiresAt);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal($"organizations/{organizationId:N}/documents/{document.Id:N}.pdf", document.BlobReference);
    }

    [Fact]
    public void CreatePendingUpload_WithNonPdfName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Document.CreatePendingUpload(
            Guid.NewGuid(),
            "handbook.docx",
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(15)));
    }

    [Fact]
    public void CompleteUpload_TransitionsPendingDocumentAndClearsUploadExpiration()
    {
        var document = Document.CreatePendingUpload(
            Guid.NewGuid(),
            "handbook.pdf",
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(15));

        document.CompleteUpload();

        Assert.Equal(DocumentStatus.Uploaded, document.Status);
        Assert.Null(document.UploadExpiresAt);
    }

    [Fact]
    public void IsUploadExpired_UsesPendingStatusAndExpiration()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var document = Document.CreatePendingUpload(
            Guid.NewGuid(),
            "handbook.pdf",
            Guid.NewGuid(),
            expiresAt);

        Assert.False(document.IsUploadExpired(expiresAt.AddTicks(-1)));
        Assert.True(document.IsUploadExpired(expiresAt));

        document.CompleteUpload();

        Assert.False(document.IsUploadExpired(expiresAt.AddMinutes(1)));
    }
}

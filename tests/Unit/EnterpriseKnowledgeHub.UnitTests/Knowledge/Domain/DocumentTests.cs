using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Domain;

public sealed class DocumentTests
{
    [Theory]
    [InlineData("handbook.pdf", "application/pdf", ".pdf")]
    [InlineData("handbook.PDF", "application/pdf", ".pdf")]
    [InlineData("notes.md", "text/markdown", ".md")]
    [InlineData("notes.MD", "text/markdown", ".md")]
    public void CreatePendingUpload_WithSupportedFormat_CreatesPendingDocumentBoundToOrganization(
        string fileName, string contentType, string extension)
    {
        var organizationId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var document = Document.CreatePendingUpload(organizationId, $"  {fileName}  ", createdBy, expiresAt);

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal(organizationId, document.OrganizationId);
        Assert.Equal(createdBy, document.CreatedBy);
        Assert.Equal(fileName, document.Name);
        Assert.Equal(DocumentStatus.PendingForUpload, document.Status);
        Assert.Equal(expiresAt, document.UploadExpiresAt);
        Assert.Equal(contentType, document.ContentType);
        Assert.Equal($"organizations/{organizationId:N}/documents/{document.Id:N}{extension}", document.BlobReference);
    }

    [Fact]
    public void CreatePendingUpload_WithUnsupportedName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Document.CreatePendingUpload(
            Guid.NewGuid(),
            "handbook.docx",
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(15)));
    }

    [Fact]
    public void CompleteUpload_RecordsETagAndPreservesDeadline()
    {
        var document = Document.CreatePendingUpload(
            Guid.NewGuid(),
            "handbook.pdf",
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(15));

        var deadline = document.UploadExpiresAt;
        document.CompleteUpload("\"0x123\"");

        Assert.Equal(DocumentStatus.Uploaded, document.Status);
        Assert.Equal(deadline, document.UploadExpiresAt);
        Assert.Equal("\"0x123\"", document.ValidatedETag);
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

        document.CompleteUpload("\"0x123\"");

        Assert.False(document.IsUploadExpired(expiresAt.AddMinutes(1)));
    }
}

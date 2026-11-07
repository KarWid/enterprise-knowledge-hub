using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentUploadCapabilities;
using EnterpriseKnowledgeHub.UnitTests.Knowledge.Fakes;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents.GetDocumentUploadCapabilities;

public sealed class GetDocumentUploadCapabilitiesQueryHandlerTests
{
    [Fact]
    public async Task Capabilities_ReturnOnlyEnabledFormatsAndConfiguredSize()
    {
        // Arrange
        var settings = new DocumentUploadOptions { MaxUploadSizeBytes = 1000 };
        settings.EnabledFormats["pdf"] = false;
        var handler = new GetDocumentUploadCapabilitiesQueryHandler(new FakeOrganizationAccessService(CreateMembership()), Options.Create(settings));

        // Act
        var result = await handler.Handle(new GetDocumentUploadCapabilitiesQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(1000, result.MaxSizeBytes);
        Assert.Equal(new DocumentUploadFormatResult("markdown", ".md", "text/markdown"), Assert.Single(result.Formats));
    }

    [Fact]
    public async Task Capabilities_RequireOrganizationMembership()
    {
        // Arrange
        var handler = new GetDocumentUploadCapabilitiesQueryHandler(new FakeOrganizationAccessService(null), Options.Create(new DocumentUploadOptions()));

        // Act
        Task GetCapabilities() => handler.Handle(new GetDocumentUploadCapabilitiesQuery(), CancellationToken.None).AsTask();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(GetCapabilities);
    }

    [Fact]
    public async Task Capabilities_CanDisableAllUploads()
    {
        // Arrange
        var settings = new DocumentUploadOptions { EnabledFormats = [] };
        var handler = new GetDocumentUploadCapabilitiesQueryHandler(new FakeOrganizationAccessService(CreateMembership()), Options.Create(settings));

        // Act
        var result = await handler.Handle(new GetDocumentUploadCapabilitiesQuery(), CancellationToken.None);

        // Assert
        Assert.Empty(result.Formats);
    }

    private static TestOrganizationMembership CreateMembership() => new(Guid.NewGuid(), Guid.NewGuid());
}

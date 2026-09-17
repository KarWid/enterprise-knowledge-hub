using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Domain.Enums;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Domain;

public class ApplicationUserTests
{
    [Fact]
    public void Create_WithValidData_SetsExpectedProperties()
    {
        var user = ApplicationUser.Create("external-id", "user@example.com", "Jane Doe");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("external-id", user.ExternalIdentityId);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("Jane Doe", user.DisplayName);
        Assert.Equal(ApplicationUserStatus.Active, user.Status);
    }

    [Theory]
    [InlineData("", "user@example.com", "Jane Doe")]
    [InlineData("external-id", "", "Jane Doe")]
    [InlineData("external-id", "user@example.com", "")]
    public void Create_WhenRequiredFieldIsEmpty_ThrowsDomainException(
        string externalIdentityId, string email, string displayName)
    {
        Assert.Throws<DomainException>(
            () => ApplicationUser.Create(externalIdentityId, email, displayName));
    }

    [Fact]
    public void Create_WhenExternalIdentityIdExceedsMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('a', 257);

        Assert.Throws<DomainException>(
            () => ApplicationUser.Create(tooLong, "user@example.com", "Jane Doe"));
    }
}

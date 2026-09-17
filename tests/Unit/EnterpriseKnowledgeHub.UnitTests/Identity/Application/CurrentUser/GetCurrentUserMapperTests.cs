using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Domain;

namespace EnterpriseKnowledgeHub.UnitTests.Identity.Application.CurrentUser;

public class GetCurrentUserMapperTests
{
    [Fact]
    public void MapToGetCurrentUserResult_MapsAllFieldsAndSetsFoundTrue()
    {
        var user = ApplicationUser.Create("external-id", "user@example.com", "Jane Doe");

        var result = GetCurrentUserMapper.MapToGetCurrentUserResult(user);

        Assert.True(result.Found);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.DisplayName, result.Name);
    }

    [Fact]
    public void NotFound_ReturnsEmptyResultWithFoundFalse()
    {
        var result = GetCurrentUserMapper.NotFound();

        Assert.False(result.Found);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.Email);
        Assert.Null(result.Name);
    }
}

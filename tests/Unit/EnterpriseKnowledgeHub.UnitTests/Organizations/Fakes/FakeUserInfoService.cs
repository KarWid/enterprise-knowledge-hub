using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;

internal sealed class FakeUserInfoService(IUserInfo userInfo) : IUserInfoService
{
    public Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken)
        => Task.FromResult(userInfo);
}

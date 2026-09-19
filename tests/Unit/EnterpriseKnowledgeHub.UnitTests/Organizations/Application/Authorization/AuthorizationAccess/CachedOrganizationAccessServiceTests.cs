using EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess;
using EnterpriseKnowledgeHub.UnitTests.Organizations.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace EnterpriseKnowledgeHub.UnitTests.Organizations.Application.Authorization.AuthorizationAccess;

public class CachedOrganizationAccessServiceTests
{
    [Fact]
    public async Task GetCurrentOrganizationMembershipAsync_CachesResultAcrossCalls()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var inner = new FakeOrganizationAccessService(
            new OrganizationMembership(Guid.NewGuid(), organizationId, userId, "Employee"));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CachedOrganizationAccessService(
            inner,
            cache,
            new FakeCurrentOrganization { OrganizationId = organizationId },
            new FakeUserInfoService(new FakeUserInfo(userId)));

        await service.GetCurrentOrganizationMembershipAsync(CancellationToken.None);
        await service.GetCurrentOrganizationMembershipAsync(CancellationToken.None);

        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task GetCurrentOrganizationMembershipAsync_WhenNoOrganizationOnRequest_SkipsCacheAndDelegatesEveryCall()
    {
        var inner = new FakeOrganizationAccessService(null);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CachedOrganizationAccessService(
            inner,
            cache,
            new FakeCurrentOrganization { OrganizationId = null },
            new FakeUserInfoService(new FakeUserInfo(Guid.NewGuid())));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentOrganizationMembershipAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentOrganizationMembershipAsync(CancellationToken.None));

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task ClearCache_RemovesCachedEntrySoInnerIsCalledAgain()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var inner = new FakeOrganizationAccessService(
            new OrganizationMembership(Guid.NewGuid(), organizationId, userId, "Employee"));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CachedOrganizationAccessService(
            inner,
            cache,
            new FakeCurrentOrganization { OrganizationId = organizationId },
            new FakeUserInfoService(new FakeUserInfo(userId)));

        await service.GetCurrentOrganizationMembershipAsync(CancellationToken.None);
        service.ClearCache(userId, organizationId);
        await service.GetCurrentOrganizationMembershipAsync(CancellationToken.None);

        Assert.Equal(2, inner.CallCount);
        Assert.Equal(1, inner.ClearCacheCallCount);
    }
}

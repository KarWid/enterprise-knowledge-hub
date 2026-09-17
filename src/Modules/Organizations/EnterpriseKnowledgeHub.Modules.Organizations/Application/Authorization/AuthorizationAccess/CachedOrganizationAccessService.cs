using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using Microsoft.Extensions.Caching.Memory;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess
{
    /// <summary>
    /// Caches resolved <see cref="IOrganizationMembership"/> per user/organization pair. No
    /// expiration: entries live for the process lifetime until explicitly cleared via
    /// <see cref="ClearCache"/> (e.g. after a membership or role change).
    /// </summary>
    public sealed class CachedOrganizationAccessService(
        IOrganizationAccessService _inner,
        IMemoryCache _cache,
        ICurrentOrganization _currentOrganization,
        IUserInfoService _userInfoService) : IOrganizationAccessService
    {
        public async Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken)
        {
            var organizationId = _currentOrganization.OrganizationId;

            // No organization on the request — let the inner service raise the standard exception.
            if (organizationId is null)
            {
                return await _inner.GetCurrentOrganizationMembershipAsync(cancellationToken);
            }

            var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);
            var cacheKey = BuildCacheKey(userInfo.UserId, organizationId.Value);

            if (_cache.TryGetValue(cacheKey, out IOrganizationMembership? cached) && cached is not null)
            {
                return cached;
            }

            var membership = await _inner.GetCurrentOrganizationMembershipAsync(cancellationToken);
            _cache.Set(cacheKey, membership);

            return membership;
        }

        public void ClearCache(Guid userId, Guid organizationId)
        {
            _cache.Remove(BuildCacheKey(userId, organizationId));
            _inner.ClearCache(userId, organizationId);
        }

        private static string BuildCacheKey(Guid userId, Guid organizationId) =>
            $"OrganizationMembership:{userId}:{organizationId}";
    }
}

using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using Microsoft.Extensions.Caching.Memory;

namespace EnterpriseKnowledgeHub.Api.Authentication
{
    /// <summary>
    /// Caches resolved <see cref="IUserInfo"/> per external identity (oid claim). No expiration:
    /// entries live for the process lifetime until evicted.
    /// </summary>
    public sealed class CachedUserInfoService(
        IUserInfoService _inner,
        IMemoryCache _cache,
        ICurrentUser _currentUser) : IUserInfoService
    {
        public async Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken)
        {
            var externalId = _currentUser.ExternalId
                ?? throw new InvalidOperationException("External identity ID is missing from the token.");

            var cacheKey = $"UserInfo:{externalId}";

            if (_cache.TryGetValue(cacheKey, out IUserInfo? cached) && cached is not null)
            {
                return cached;
            }

            var userInfo = await _inner.GetUserInfoAsync(cancellationToken);
            _cache.Set(cacheKey, userInfo);

            return userInfo;
        }
    }
}

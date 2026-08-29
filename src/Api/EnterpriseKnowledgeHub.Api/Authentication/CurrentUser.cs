using System.Security.Claims;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.Api.Authentication
{
    public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        private ClaimsPrincipal User =>
            httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("HttpContext is not available.");

        public string ExternalId =>
            User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier") ?? throw new UnauthorizedAccessException("Missing oid claim.");

        public string? Email =>
            User.FindFirstValue("preferred_username")
            ?? User.FindFirstValue(ClaimTypes.Email);

        public string? Name =>
            User.FindFirstValue("name");
    }
}

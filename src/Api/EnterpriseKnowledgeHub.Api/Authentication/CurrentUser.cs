using System.Security.Claims;
using EnterpriseKnowledgeHub.BuildingBlocks.Application.Abstractions;

namespace EnterpriseKnowledgeHub.Api.Authentication
{
    public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        private ClaimsPrincipal User =>
            httpContextAccessor.HttpContext?.User
            ?? new ClaimsPrincipal();

        public string? ExternalId =>
            User.FindFirstValue("oid");

        public string? Email =>
            User.FindFirstValue("preferred_username")
            ?? User.FindFirstValue(ClaimTypes.Email);

        public string? Name =>
            User.FindFirstValue("name");
    }
}

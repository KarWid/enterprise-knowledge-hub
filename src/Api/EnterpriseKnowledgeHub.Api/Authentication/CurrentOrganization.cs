using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.Api.Authentication
{
    public sealed class CurrentOrganization(IHttpContextAccessor httpContextAccessor) : ICurrentOrganization
    {
        private const string OrganizationHeaderName = "X-Organization-Id";

        public Guid? OrganizationId
        {
            get
            {
                var headerValue = httpContextAccessor.HttpContext?.Request.Headers[OrganizationHeaderName]
                    .FirstOrDefault();

                return Guid.TryParse(headerValue, out var organizationId) ? organizationId : null;
            }
        }
    }
}

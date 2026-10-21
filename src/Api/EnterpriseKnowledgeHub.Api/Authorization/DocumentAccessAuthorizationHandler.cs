using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseKnowledgeHub.Api.Authorization;

public sealed class DocumentAccessAuthorizationHandler(IOrganizationAccessService organizationAccessService)
    : AuthorizationHandler<DocumentAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentAccessRequirement requirement)
    {
        try
        {
            var membership = await organizationAccessService
                .GetCurrentOrganizationMembershipAsync(CancellationToken.None);

            if (requirement.Allows(membership.Role))
                context.Succeed(requirement);
        }
        catch (OrganizationAccessDeniedException)
        {
            context.Fail();
        }
    }
}

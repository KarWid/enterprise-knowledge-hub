using EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview;
using EnterpriseKnowledgeHub.Contracts.Enums;
using EnterpriseKnowledgeHub.Contracts.Identity.GetMe;
using EnterpriseKnowledgeHub.Modules.Identity.Enums;

using OrganizationRoleContracts = EnterpriseKnowledgeHub.Contracts.Enums.OrganizationRole;
using OrganizationRoleDomain = EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums.OrganizationRole;

namespace EnterpriseKnowledgeHub.Api.Mappers
{
    internal static class CurrentUserMapper
    {
        internal static CurrentUserResponse MapToCurrentUserResponse(
            GetCurrentUserOverviewQueryResult currentUserResult)
        {
            return new CurrentUserResponse(
                currentUserResult.Id,
                currentUserResult.Email,
                currentUserResult.Name,
                MapToUserOnboardingStatus(currentUserResult.OnboardingState),
                [.. currentUserResult.PendingInvitations.Select(MapToMePendingInvitationItem)],
                [.. currentUserResult.Organizations.Select(MapToMeOrganizationItem)]);
        }

        internal static UserOnboardingStatus MapToUserOnboardingStatus(
            UserOnboardingState state) =>
            state switch
            {
                UserOnboardingState.Complete
                    => UserOnboardingStatus.Complete,

                UserOnboardingState.CreateOrganization
                    => UserOnboardingStatus.CreateOrganization,

                UserOnboardingState.AcceptInvitation
                    => UserOnboardingStatus.AcceptInvitation,

                UserOnboardingState.AccessDenied
                    => UserOnboardingStatus.AccessDenied,

                _ => throw new ArgumentOutOfRangeException(nameof(state))
            };

        private static MePendingInvitationItem MapToMePendingInvitationItem(this PendingInvitationOverviewItem item) =>
            new MePendingInvitationItem(item.Id, item.OrganizationId, item.OrganizationName, item.ExpiresAt);

        private static MeOrganizationItem MapToMeOrganizationItem(this OrganizationOverviewItem item) =>
            new MeOrganizationItem(item.Id, item.Name, MapToOrganizationRoleContracts(item.Role));

        private static OrganizationRoleContracts MapToOrganizationRoleContracts(this OrganizationRoleDomain organizationRole) =>
            organizationRole switch
            {
                OrganizationRoleDomain.Employee => OrganizationRoleContracts.Employee,
                OrganizationRoleDomain.KnowledgeManager => OrganizationRoleContracts.KnowledgeManager,
                OrganizationRoleDomain.OrganizationOwner => OrganizationRoleContracts.OrganizationOwner,
                OrganizationRoleDomain.OrganizationAdmin => OrganizationRoleContracts.OrganizationAdmin,
                _ => throw new ArgumentOutOfRangeException(nameof(organizationRole))
            };
    }
}
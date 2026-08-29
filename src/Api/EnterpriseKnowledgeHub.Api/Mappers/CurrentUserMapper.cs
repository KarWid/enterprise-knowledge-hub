using EnterpriseKnowledgeHub.Contracts.Enums;
using EnterpriseKnowledgeHub.Contracts.Identity;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using EnterpriseKnowledgeHub.Modules.Identity.Enums;

namespace EnterpriseKnowledgeHub.Api.Mappers
{
    internal static class CurrentUserMapper
    {
        internal static CurrentUserResponse MapToCurrentUserResponse(
            GetCurrentUserResult currentUserResult)
        {
            return new CurrentUserResponse(
                currentUserResult.Id, 
                currentUserResult.Email, 
                currentUserResult.Name, 
                MapToUserOnboardingStatus(currentUserResult.OnboardingState));
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

                _ => throw new ArgumentOutOfRangeException(nameof(state))
            };
    }
}
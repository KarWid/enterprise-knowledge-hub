using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Enums;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser
{
    internal static class GetCurrentUserMapper
    {
        internal static GetCurrentUserResult MapToGetCurrentUserResult(
            ApplicationUser applicationUser,
            UserOnboardingState onboardingState)
        {
            return new GetCurrentUserResult(
                applicationUser.Id, 
                applicationUser.Email, 
                applicationUser.DisplayName, 
                onboardingState);
        }
    }
}

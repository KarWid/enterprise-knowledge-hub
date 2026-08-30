using EnterpriseKnowledgeHub.Modules.Identity.Domain;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser
{
    internal static class GetCurrentUserMapper
    {
        internal static GetCurrentUserResult MapToGetCurrentUserResult(
            ApplicationUser applicationUser)
        {
            return new GetCurrentUserResult(
                applicationUser.Id, 
                applicationUser.Email, 
                applicationUser.DisplayName);
        }
    }
}

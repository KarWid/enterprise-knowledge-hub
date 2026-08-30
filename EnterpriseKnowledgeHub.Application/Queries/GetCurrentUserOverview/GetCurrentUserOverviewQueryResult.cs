using EnterpriseKnowledgeHub.Modules.Identity.Enums;

namespace EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview
{
    public sealed record GetCurrentUserOverviewQueryResult(
        Guid Id,
        string? Email,
        string? Name,
        UserOnboardingState OnboardingState);
}

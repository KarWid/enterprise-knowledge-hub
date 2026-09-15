using EnterpriseKnowledgeHub.Modules.Identity.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview
{
    public sealed record PendingInvitationOverviewItem(
        Guid Id,
        Guid OrganizationId,
        string OrganizationName,
        DateTime ExpiresAt);

    public sealed record OrganizationOverviewItem(Guid Id, string Name, OrganizationRole Role);

    public sealed record GetCurrentUserOverviewQueryResult(
        Guid Id,
        string? Email,
        string? Name,
        UserOnboardingState OnboardingState,
        IReadOnlyList<PendingInvitationOverviewItem> PendingInvitations,
        IReadOnlyList<OrganizationOverviewItem> Organizations);
}

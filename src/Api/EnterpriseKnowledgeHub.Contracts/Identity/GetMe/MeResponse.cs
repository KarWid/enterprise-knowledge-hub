using EnterpriseKnowledgeHub.Contracts.Enums;

namespace EnterpriseKnowledgeHub.Contracts.Identity.GetMe;

public sealed record CurrentUserResponse(
    Guid Id, 
    string? Email, 
    string? Name, 
    UserOnboardingStatus OnboardingStatus,
    IReadOnlyList<MePendingInvitationItem> PendingInvitations,
    IReadOnlyList<MeOrganizationItem> Organizations);

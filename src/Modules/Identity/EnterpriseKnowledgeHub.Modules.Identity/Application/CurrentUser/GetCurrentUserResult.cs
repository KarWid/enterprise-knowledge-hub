using EnterpriseKnowledgeHub.Modules.Identity.Enums;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

public sealed record GetCurrentUserResult(
    Guid Id, 
    string? Email, 
    string? Name, 
    UserOnboardingState OnboardingState);

using EnterpriseKnowledgeHub.Contracts.Enums;

namespace EnterpriseKnowledgeHub.Contracts.Identity;

public sealed record CurrentUserResponse(
    Guid Id, 
    string? Email, 
    string? Name, 
    UserOnboardingStatus OnboardingStatus);

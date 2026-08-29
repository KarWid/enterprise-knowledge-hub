using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Domain.Exceptions;

namespace EnterpriseKnowledgeHub.Modules.Identity.Domain;

public sealed class ApplicationUser
{
    public Guid Id { get; private set; }
    public string ExternalIdentityId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public ApplicationUserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApplicationUser() { }

    public static ApplicationUser Create(string externalIdentityId, string email, string displayName)
    {
        DomainGuard.Required(externalIdentityId, nameof(ExternalIdentityId), 256);
        DomainGuard.Required(email, nameof(Email), 256);
        DomainGuard.Required(displayName, nameof(DisplayName), 256);

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ExternalIdentityId = externalIdentityId,
            Email = email,
            DisplayName = displayName,
            Status = ApplicationUserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }
}

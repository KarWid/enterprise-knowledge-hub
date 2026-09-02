using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Domain;

public sealed class OrganizationOwnerInvitation
{
    private OrganizationOwnerInvitation()
    {
    }

    private OrganizationOwnerInvitation(
        Guid id,
        string email,
        string tokenHash,
        DateTime expiresAt)
    {
        Id = id;
        Email = email;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public InvitationStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? AcceptedAt { get; private set; }

    public static OrganizationOwnerInvitation Create(
        string email,
        string tokenHash,
        DateTime expiresAt)
    {
        // TODO @KWidla: validation
        return new OrganizationOwnerInvitation(
            Guid.NewGuid(),
            email,
            tokenHash,
            expiresAt);
    }

    public void Accept()
    {
        // TODO @KWidla: validation
        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        // TODO @KWidla: validation
        Status = InvitationStatus.Revoked;
    }
}
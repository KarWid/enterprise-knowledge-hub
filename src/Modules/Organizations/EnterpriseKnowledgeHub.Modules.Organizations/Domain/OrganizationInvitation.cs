using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Domain;

public sealed class OrganizationInvitation
{
    private OrganizationInvitation()
    {
    }

    private OrganizationInvitation(
        Guid id,
        Guid organizationId,
        string email,
        string tokenHash,
        Guid invitedByUserId,
        DateTime expiresAt)
    {
        Id = id;
        OrganizationId = organizationId;
        Email = email;
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Email { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public InvitationStatus Status { get; private set; }

    public Guid InvitedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? AcceptedAt { get; private set; }

    public static OrganizationInvitation Create(
        Guid organizationId,
        string email,
        string tokenHash,
        Guid invitedByUserId,
        DateTime expiresAt)
    {
        // TODO @KWidla: validate email
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException(
                "Invitation expiration date must be in the future.",
                nameof(expiresAt));

        return new OrganizationInvitation(
            Guid.NewGuid(),
            organizationId,
            email.Trim().ToLowerInvariant(),
            tokenHash,
            invitedByUserId,
            expiresAt);
    }

    public void Accept()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException(
                "Only pending invitations can be accepted.");

        if (IsExpired())
        {
            // TODO @KWidla

            Status = InvitationStatus.Expired;

            throw new InvalidOperationException(
                "Invitation has expired.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException(
                "Only pending invitations can be revoked.");

        Status = InvitationStatus.Revoked;
    }

    public bool IsExpired()
        => Status == InvitationStatus.Pending &&
           ExpiresAt <= DateTime.UtcNow;
}
namespace EnterpriseKnowledgeHub.Modules.Organizations.Domain;

public sealed class Membership
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public OrganizationRole Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Membership() { }

    internal static Membership Create(Guid userId, Guid organizationId, OrganizationRole role)
    {
        return new Membership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            Role = role,
            Status = MembershipStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }
}

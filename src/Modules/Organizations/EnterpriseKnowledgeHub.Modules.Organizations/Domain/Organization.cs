using EnterpriseKnowledgeHub.BuildingBlocks.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Domain;

public sealed class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public OrganizationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Membership> _memberships = [];
    public IReadOnlyCollection<Membership> Memberships => _memberships.AsReadOnly();

    private Organization() { }

    public static Organization Create(string name)
    {
        DomainGuard.Required(name, nameof(Name), 256);

        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Status = OrganizationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddOwner(Guid userId)
    {
        _memberships.Add(Membership.Create(userId, Id, OrganizationRole.OrganizationOwner));
    }
}

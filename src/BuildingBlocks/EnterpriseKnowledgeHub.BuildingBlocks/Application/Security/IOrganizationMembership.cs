namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    /// <summary>
    /// Resolved membership of the current user in the organization the request is scoped to.
    /// </summary>
    public interface IOrganizationMembership
    {
        Guid MembershipId { get; }
        Guid OrganizationId { get; }
        Guid UserId { get; }
        string Role { get; }
    }
}

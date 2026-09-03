namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    /// <summary>
    /// Resolved application-level identity for the current request: internal UserId,
    /// the organization the user is currently assigned to.
    /// </summary>
    public interface IUserInfo
    {
        Guid UserId { get; }
        IReadOnlyCollection<Guid>? OrganizationIds { get; }
    }
}

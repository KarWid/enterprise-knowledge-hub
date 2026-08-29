namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    /// <summary>
    /// Handles JWT information
    /// </summary>
    public interface ICurrentUser
    {
        string? ExternalId { get; }
        string? Email { get; }
        string? Name { get; }
    }
}

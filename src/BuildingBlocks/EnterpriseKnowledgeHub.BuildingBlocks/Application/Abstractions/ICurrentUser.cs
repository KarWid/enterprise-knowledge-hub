namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Abstractions
{
    public interface ICurrentUser
    {
        string? ExternalId { get; }
        string? Email { get; }
        string? Name { get; }
    }
}

namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    public interface IUserContext
    {
        Task<Guid> GetUserIdAsync(CancellationToken cancellationToken);
    }
}

namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    public interface IUserContext
    {
        Task<Guid> GetUserIdAsync(CancellationToken cancellationToken);
        Task<string> GetUserEmailAsync(CancellationToken cancellationToken);
        Task<string> GetUserNameAsync(CancellationToken cancellationToken);
    }
}

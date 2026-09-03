namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    /// <summary>
    /// Resolves <see cref="IUserInfo"/> for the current request. Implementations may hit the
    /// database; consumers should not assume the call is free.
    /// </summary>
    public interface IUserInfoService
    {
        Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken);
    }
}

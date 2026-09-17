namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    /// <summary>
    /// Verifies the current user has access to the organization the request is scoped to
    /// (<see cref="ICurrentOrganization"/>). Implementations may hit the database; consumers
    /// should not assume the call is free.
    /// </summary>
    public interface IOrganizationAccessService
    {
        /// <summary>
        /// Resolves the current user's membership in the current organization, or throws if there
        /// is no organization on the request, or the user does not belong to it.
        /// </summary>
        Task<IOrganizationMembership> GetCurrentOrganizationMembershipAsync(CancellationToken cancellationToken);

        /// <summary>Invalidates any cached membership for the given user/organization pair.</summary>
        void ClearCache(Guid userId, Guid organizationId);
    }
}

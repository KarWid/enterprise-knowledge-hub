namespace EnterpriseKnowledgeHub.BuildingBlocks.Application.Security
{
    /// <summary>
    /// The organization the current request is scoped to, as supplied by the client via the
    /// X-Organization-Id header. This is a hint only — <see cref="IOrganizationAccessService"/>
    /// must still verify the current user actually belongs to it.
    /// </summary>
    public interface ICurrentOrganization
    {
        Guid? OrganizationId { get; }
    }
}

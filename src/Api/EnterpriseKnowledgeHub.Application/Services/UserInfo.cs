using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;

namespace EnterpriseKnowledgeHub.Application.Services;

public sealed record UserInfo(Guid UserId, IReadOnlyCollection<Guid>? OrganizationIds) : IUserInfo;

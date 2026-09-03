using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetUserOrganizations;

internal sealed class GetUserOrganizationsQueryHandler(
    IUserInfoService _userInfoService, 
    OrganizationsDbContext _db)
    : IRequestHandler<GetUserOrganizationsQuery, GetUserOrganizationsResult>
{
    public async Task<GetUserOrganizationsResult> Handle(
        GetUserOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);
        var currentUserId = userInfo.UserId;

        var organizations = await (
            from m in _db.Memberships
            join o in _db.Organizations on m.OrganizationId equals o.Id
            where m.UserId == currentUserId && m.Status == MembershipStatus.Active
            select new UserOrganizationItem(o.Id, o.Name, o.CreatedAt, m.Role)
        ).ToListAsync(cancellationToken);

        return new GetUserOrganizationsResult(organizations);
    }
}

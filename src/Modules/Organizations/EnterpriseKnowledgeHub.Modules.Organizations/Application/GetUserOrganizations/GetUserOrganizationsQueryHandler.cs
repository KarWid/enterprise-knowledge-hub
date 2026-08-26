using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.GetUserOrganizations;

internal sealed class GetUserOrganizationsQueryHandler(OrganizationsDbContext db)
    : IRequestHandler<GetUserOrganizationsQuery, GetUserOrganizationsResult>
{
    public async Task<GetUserOrganizationsResult> Handle(
        GetUserOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var organizations = await (
            from m in db.Memberships
            join o in db.Organizations on m.OrganizationId equals o.Id
            where m.UserId == request.UserId && m.Status == MembershipStatus.Active
            select new UserOrganizationItem(o.Id, o.Name, o.CreatedAt, m.Role)
        ).ToListAsync(cancellationToken);

        return new GetUserOrganizationsResult(organizations);
    }
}

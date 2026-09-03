using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipsForUser;

internal sealed class GetMembershipsForUserQueryHandler(OrganizationsDbContext _db)
    : IRequestHandler<GetMembershipsForUserQuery, GetMembershipsForUserQueryResult>
{
    public async Task<GetMembershipsForUserQueryResult> Handle(
        GetMembershipsForUserQuery request, CancellationToken cancellationToken)
    {
        var membershipIds = await _db.Memberships
            .Where(m => m.UserId == request.UserId && m.Status == MembershipStatus.Active)
            .Select(m => m.OrganizationId)
            .ToListAsync(cancellationToken);

        return new GetMembershipsForUserQueryResult(membershipIds);
    }
}

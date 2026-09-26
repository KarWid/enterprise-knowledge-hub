using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetMembershipForUserAndOrganization;

internal sealed class GetMembershipForUserAndOrganizationQueryHandler(OrganizationsDbContext _db)
    : IRequestHandler<GetMembershipForUserAndOrganizationQuery, GetMembershipForUserAndOrganizationResult>
{
    public async ValueTask<GetMembershipForUserAndOrganizationResult> Handle(
        GetMembershipForUserAndOrganizationQuery request, CancellationToken cancellationToken)
    {
        var membership = await _db.Memberships
            .Where(m => m.UserId == request.UserId
                && m.OrganizationId == request.OrganizationId
                && m.Status == MembershipStatus.Active)
            .Select(m => new { m.Id, m.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
            return new GetMembershipForUserAndOrganizationResult(false, Guid.Empty, default);

        return new GetMembershipForUserAndOrganizationResult(true, membership.Id, membership.Role);
    }
}

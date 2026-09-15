using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.GetPendingOrganizationInvitationsForEmail;

internal sealed class GetPendingOrganizationInvitationsForEmailQueryHandler(OrganizationsDbContext _db)
    : IRequestHandler<GetPendingOrganizationInvitationsForEmailQuery, GetPendingOrganizationInvitationsForEmailResult>
{
    public async Task<GetPendingOrganizationInvitationsForEmailResult> Handle(
        GetPendingOrganizationInvitationsForEmailQuery request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var invitations = await _db.OrganizationInvitations
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .Select(i => new PendingOrganizationInvitationItem(i.Id, i.OrganizationId, string.Empty, i.ExpiresAt))
            .ToListAsync(cancellationToken);

        return new GetPendingOrganizationInvitationsForEmailResult(invitations);
    }
}

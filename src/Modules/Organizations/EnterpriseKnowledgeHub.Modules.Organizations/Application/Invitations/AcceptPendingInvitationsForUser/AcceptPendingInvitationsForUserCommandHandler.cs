using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptPendingInvitationsForUser;

internal sealed class AcceptPendingInvitationsForUserCommandHandler(OrganizationsDbContext _db)
    : IRequestHandler<AcceptPendingInvitationsForUserCommand>
{
    public async Task Handle(AcceptPendingInvitationsForUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var ownerInvitations = await _db.OrganizationOwnerInvitations
            .Where(x => x.Email == email && x.Status == InvitationStatus.Pending && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var ownerInvitation in ownerInvitations)
        {
            ownerInvitation.Accept();
        }

        var organizationInvitations = await _db.OrganizationInvitations
            .Where(x => x.Email == email && x.Status == InvitationStatus.Pending && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var organizationInvitation in organizationInvitations)
        {
            var organization = await _db.Organizations
                .Include(o => o.Memberships)
                .FirstOrDefaultAsync(o => o.Id == organizationInvitation.OrganizationId, cancellationToken);

            if (organization is null)
                continue;

            organization.AddMember(request.UserId, OrganizationRole.Employee);
            organizationInvitation.Accept();
        }

        if (ownerInvitations.Count > 0 || organizationInvitations.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

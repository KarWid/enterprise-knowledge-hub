using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.HasPendingInvitationForEmail;

internal sealed class HasPendingInvitationForEmailQueryHandler(OrganizationsDbContext _db)
    : IRequestHandler<HasPendingInvitationForEmailQuery, bool>
{
    public async Task<bool> Handle(HasPendingInvitationForEmailQuery request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var hasOwnerInvitation = await _db.OrganizationOwnerInvitations
            .AnyAsync(x => x.Email == email && x.Status == InvitationStatus.Pending && x.ExpiresAt > now, cancellationToken);

        if (hasOwnerInvitation)
            return true;

        return await _db.OrganizationInvitations
            .AnyAsync(x => x.Email == email && x.Status == InvitationStatus.Pending && x.ExpiresAt > now, cancellationToken);
    }
}

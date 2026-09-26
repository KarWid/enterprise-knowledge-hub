using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.AcceptOrganizationInvitation;

internal sealed class AcceptOrganizationInvitationCommandHandler(
    OrganizationsDbContext _db,
    IUserInfoService _userInfoService,
    ICurrentUser _currentUser)
    : IRequestHandler<AcceptOrganizationInvitationCommand, AcceptOrganizationInvitationResult>
{
    private const OrganizationRole InvitedRole = OrganizationRole.Employee;

    public async ValueTask<AcceptOrganizationInvitationResult> Handle(
        AcceptOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var email = (_currentUser.Email ?? string.Empty).Trim().ToLowerInvariant();

        var invitation = await _db.OrganizationInvitations
            .FirstOrDefaultAsync(x => x.Id == request.InvitationId, cancellationToken);

        // Deliberately generic: don't reveal whether an invitation exists for another email.
        if (invitation is null || invitation.Email != email || invitation.Status != InvitationStatus.Pending || invitation.IsExpired())
            throw new OrganizationsDomainException("Invitation not found or no longer valid.");

        var organization = await _db.Organizations
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.Id == invitation.OrganizationId, cancellationToken);

        if (organization is null)
            throw new OrganizationsDomainException("Invitation not found or no longer valid.");

        var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);

        organization.AddMember(userInfo.UserId, InvitedRole);
        invitation.Accept();

        await _db.SaveChangesAsync(cancellationToken);

        return new AcceptOrganizationInvitationResult(organization.Id, organization.Name, InvitedRole.ToString());
    }
}

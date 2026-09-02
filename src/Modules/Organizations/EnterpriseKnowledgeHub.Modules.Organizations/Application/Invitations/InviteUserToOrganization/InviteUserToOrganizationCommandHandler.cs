using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Events.OrganizationInvitationCreated;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Invitations.InviteUserToOrganization;

internal sealed class InviteUserToOrganizationCommandHandler(
    OrganizationsDbContext _db,
    IUserContext _userContext,
    IPublisher _publisher)
    : IRequestHandler<InviteUserToOrganizationCommand, InviteUserToOrganizationResult>
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<InviteUserToOrganizationResult> Handle(
        InviteUserToOrganizationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = await _userContext.GetUserIdAsync(cancellationToken);

        var isOwner = await _db.Memberships.AnyAsync(
            x => x.UserId == currentUserId
                && x.OrganizationId == request.OrganizationId
                && x.Status == MembershipStatus.Active
                && x.Role == OrganizationRole.OrganizationOwner,
            cancellationToken);

        if (!isOwner)
            throw new OrganizationsDomainException("Only organization owners can invite users.");

        var email = request.Email.Trim().ToLowerInvariant();

        var hasPendingInvitation = await _db.OrganizationInvitations.AnyAsync(
            x => x.OrganizationId == request.OrganizationId
                && x.Email == email
                && x.Status == InvitationStatus.Pending,
            cancellationToken);

        if (hasPendingInvitation)
            throw new OrganizationsDomainException("An invitation for this email is already pending.");

        var tokenHash = GenerateTokenHash();
        var expiresAt = DateTime.UtcNow.Add(InvitationLifetime);

        var invitation = OrganizationInvitation.Create(
            request.OrganizationId,
            email,
            tokenHash,
            currentUserId,
            expiresAt);

        _db.OrganizationInvitations.Add(invitation);
        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new OrganizationInvitationCreatedEvent(
                invitation.Id,
                invitation.Email,
                "TODO Organization Name",
                "TODO Inviter Name",
                "TODO Url"), cancellationToken);

        return new InviteUserToOrganizationResult(invitation.Id, invitation.OrganizationId, invitation.Email, invitation.ExpiresAt);
    }

    private static string GenerateTokenHash()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }
}

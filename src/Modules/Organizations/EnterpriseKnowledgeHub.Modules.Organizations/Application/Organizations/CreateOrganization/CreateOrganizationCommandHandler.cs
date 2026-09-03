using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain.Enums;
using EnterpriseKnowledgeHub.Modules.Organizations.Exceptions;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler(
    OrganizationsDbContext _db,
    IUserInfoService _userInfoService,
    ICurrentUser _currentUser)
    : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResult>
{
    public async Task<CreateOrganizationResult> Handle(
        CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);
        var currentUserId = userInfo.UserId;

        var isUserAdminAlready = _db.Memberships.Any(x => x.UserId == currentUserId && x.Status == MembershipStatus.Active && x.Role == OrganizationRole.OrganizationOwner);
        if (isUserAdminAlready)
        {
            throw new OrganizationsDomainException("User is already an admin of the registered company and can not create another one");
        }

        var currentUserEmail = (_currentUser.Email ?? string.Empty).Trim().ToLowerInvariant();

        var isInvitedOwner = _db.OrganizationOwnerInvitations.Any(
            x => x.Email == currentUserEmail && x.Status == InvitationStatus.Accepted);
        if (!isInvitedOwner)
        {
            throw new OrganizationsDomainException("Only invited users can create an organization.");
        }

        var organization = Organization.Create(request.Name);
        organization.AddOwner(currentUserId);

        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationResult(organization.Id, organization.Name);
    }
}

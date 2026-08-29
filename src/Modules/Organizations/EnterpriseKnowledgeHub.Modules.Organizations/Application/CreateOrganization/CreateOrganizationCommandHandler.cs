using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler(
    OrganizationsDbContext _db,
    IUserContext _userContext)
    : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResult>
{
    public async Task<CreateOrganizationResult> Handle(
        CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = await _userContext.GetUserIdAsync(cancellationToken);

        var organization = Organization.Create(request.Name);
        organization.AddOwner(currentUserId);

        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationResult(organization.Id, organization.Name);
    }
}

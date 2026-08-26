using EnterpriseKnowledgeHub.Modules.Organizations.Domain;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;
using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Organizations.Application.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler(OrganizationsDbContext db)
    : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResult>
{
    public async Task<CreateOrganizationResult> Handle(
        CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = Organization.Create(request.Name);
        organization.AddOwner(request.UserId);

        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationResult(organization.Id, organization.Name);
    }
}

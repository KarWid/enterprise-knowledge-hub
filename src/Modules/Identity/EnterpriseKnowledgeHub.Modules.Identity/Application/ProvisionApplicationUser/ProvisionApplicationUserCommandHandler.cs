using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.ProvisionApplicationUser;

internal sealed class ProvisionApplicationUserCommandHandler(IdentityDbContext db)
    : IRequestHandler<ProvisionApplicationUserCommand, ProvisionApplicationUserResult>
{
    public async Task<ProvisionApplicationUserResult> Handle(
        ProvisionApplicationUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.ExternalIdentityId == request.ExternalIdentityId, cancellationToken);

        if (existing is not null)
        {
            return new ProvisionApplicationUserResult(existing.Id, existing.Email, existing.DisplayName);
        }

        var user = ApplicationUser.Create(request.ExternalIdentityId, request.Email, request.DisplayName);

        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new ProvisionApplicationUserResult(user.Id, user.Email, user.DisplayName);
    }
}

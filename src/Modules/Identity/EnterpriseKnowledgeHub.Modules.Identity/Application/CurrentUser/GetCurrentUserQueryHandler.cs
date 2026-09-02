using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Identity.Domain;
using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;

internal sealed class GetCurrentUserQueryHandler(
    IdentityDbContext db, 
    ICurrentUser currentUser)
    : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult>
{
    public async Task<GetCurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(currentUser.ExternalId))
            throw new InvalidOperationException("External identity ID is missing from the token.");

        var user = await db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.ExternalIdentityId == currentUser.ExternalId, cancellationToken);

        // Provisioning of a new ApplicationUser is gated by invitations; this query never creates one.
        return user is not null
            ? GetCurrentUserMapper.MapToGetCurrentUserResult(user)
            : GetCurrentUserMapper.NotFound();
    }
}

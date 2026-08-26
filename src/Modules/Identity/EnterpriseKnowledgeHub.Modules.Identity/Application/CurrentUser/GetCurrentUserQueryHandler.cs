using EnterpriseKnowledgeHub.BuildingBlocks.Application.Abstractions;
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

        if (user is null)
        {
            user = ApplicationUser.Create(
                currentUser.ExternalId,
                currentUser.Email ?? string.Empty,
                currentUser.Name ?? string.Empty);

            db.ApplicationUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new GetCurrentUserResult(user.Id, user.Email, user.DisplayName);
    }
}

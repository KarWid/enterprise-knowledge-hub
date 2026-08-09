using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.Health;

internal sealed class GetHealthQueryHandler(IdentityDbContext db)
    : IRequestHandler<GetHealthQuery, GetHealthResult>
{
    public async Task<GetHealthResult> Handle(GetHealthQuery request, CancellationToken cancellationToken)
    {
        var isHealthy = await db.Database.CanConnectAsync(cancellationToken);
        return new GetHealthResult(isHealthy);
    }
}

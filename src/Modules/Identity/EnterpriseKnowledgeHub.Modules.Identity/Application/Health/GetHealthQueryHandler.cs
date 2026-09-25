using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.Health;

internal sealed class GetHealthQueryHandler(
    IdentityDbContext db,
    ILogger<GetHealthQueryHandler> logger)
    : IRequestHandler<GetHealthQuery, GetHealthResult>
{
    public async Task<GetHealthResult> Handle(GetHealthQuery request, CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await db.Database.CloseConnectionAsync();

            return new GetHealthResult(true);
        }
        catch (Exception exception)
        {
            // Keep connection details out of the HTTP response, but make the
            // underlying exception available to trusted application logs.
            logger.LogError(exception, "The health check could not connect to the database.");

            return new GetHealthResult(false);
        }
    }
}

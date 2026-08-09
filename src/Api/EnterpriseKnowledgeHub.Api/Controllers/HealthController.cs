using EnterpriseKnowledgeHub.Contracts.Health;
using EnterpriseKnowledgeHub.Modules.Identity.Application.Health;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetHealthQuery(), cancellationToken);
        return Ok(new HealthResponse(
            Status: result.IsHealthy ? "healthy" : "degraded",
            Database: result.IsHealthy ? "healthy" : "unavailable"));
    }
}

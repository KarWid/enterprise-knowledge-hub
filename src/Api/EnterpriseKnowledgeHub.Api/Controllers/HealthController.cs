using EnterpriseKnowledgeHub.Contracts.Health;
using EnterpriseKnowledgeHub.Contracts.Errors;
using EnterpriseKnowledgeHub.Modules.Identity.Application.Health;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(OperationId = "GetHealth")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetHealthQuery(), cancellationToken);
        return Ok(new HealthResponse(
            Status: result.IsHealthy ? "healthy" : "degraded",
            Database: result.IsHealthy ? "healthy" : "unavailable"));
    }
}

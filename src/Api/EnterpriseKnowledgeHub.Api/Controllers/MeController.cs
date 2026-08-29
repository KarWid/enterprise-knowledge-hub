using EnterpriseKnowledgeHub.Contracts.Identity;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[Route("api/me")]
[Authorize]
public class MeController(IMediator _mediator) : ApiControllerBase()
{
    [HttpGet]
    [SwaggerOperation(OperationId = "GetMe")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(new MeResponse(result.Id, result.Email, result.Name));
    }
}

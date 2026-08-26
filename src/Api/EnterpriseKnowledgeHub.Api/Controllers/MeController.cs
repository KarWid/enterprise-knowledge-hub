using EnterpriseKnowledgeHub.BuildingBlocks.Application.Abstractions;
using EnterpriseKnowledgeHub.Contracts.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[Route("api/me")]
[Authorize]
public class MeController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await GetCurrentUserAsync(cancellationToken);
        return Ok(new MeResponse(result.Id, result.Email, result.Name));
    }
}

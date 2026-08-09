using System.Security.Claims;
using EnterpriseKnowledgeHub.Contracts.Identity;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserQuery(
            ExternalId: User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email: User.FindFirstValue("preferred_username") ?? User.FindFirstValue(ClaimTypes.Email),
            Name: User.FindFirstValue("name"));

        var result = await mediator.Send(query, cancellationToken);
        return Ok(new MeResponse(result.Id, result.Email, result.Name));
    }
}

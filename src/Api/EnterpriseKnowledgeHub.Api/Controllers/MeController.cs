using System.Net;
using EnterpriseKnowledgeHub.Api.Mappers;
using EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview;
using EnterpriseKnowledgeHub.Contracts.Identity;
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
    [ProducesResponseType(typeof(CurrentUserResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserOverviewQuery(), cancellationToken);
        return Ok(CurrentUserMapper.MapToCurrentUserResponse(result));
    }
}

using EnterpriseKnowledgeHub.Contracts.Organizations;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.CreateOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Organizations.GetUserOrganizations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[Route("api/organizations")]
[Authorize]
public class OrganizationsController(IMediator _mediator) : ApiControllerBase()
{
    [HttpGet]
    [SwaggerOperation(OperationId = "GetOrganizations")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserOrganizationsQuery(), cancellationToken);

        var response = result.Organizations
            .Select(o => new OrganizationResponse(o.Id, o.Name, o.Role.ToString()))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [SwaggerOperation(OperationId = "CreateOrganization")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateOrganizationCommand(request.Name),
            cancellationToken);

        var response = new OrganizationResponse(result.Id, result.Name, "OrganizationOwner"); // TODO @KWidla

        return CreatedAtAction(nameof(Get), response);
    }
}

using EnterpriseKnowledgeHub.Contracts.Organizations;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.CreateOrganization;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.GetUserOrganizations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[Route("api/organizations")]
[Authorize]
public class OrganizationsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);

        var result = await Mediator.Send(new GetUserOrganizationsQuery(userId), cancellationToken);

        var response = result.Organizations
            .Select(o => new OrganizationResponse(o.Id, o.Name, o.Role.ToString()))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Organization name is required.");

        var userId = await ResolveUserIdAsync(cancellationToken);

        var result = await Mediator.Send(
            new CreateOrganizationCommand(userId, request.Name.Trim()),
            cancellationToken);

        var response = new OrganizationResponse(result.Id, result.Name, "OrganizationOwner");

        return CreatedAtAction(nameof(Get), response);
    }

    private async Task<Guid> ResolveUserIdAsync(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user.Id;
    }
}

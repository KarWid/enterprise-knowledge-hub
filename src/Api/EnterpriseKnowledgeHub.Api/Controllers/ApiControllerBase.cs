using System.Security.Claims;
using EnterpriseKnowledgeHub.Modules.Identity.Application.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected Task<GetCurrentUserResult> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        Mediator.Send(new GetCurrentUserQuery(), cancellationToken);
}

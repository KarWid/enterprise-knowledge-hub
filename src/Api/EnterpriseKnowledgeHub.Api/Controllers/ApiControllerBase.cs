using Microsoft.AspNetCore.Mvc;
using EnterpriseKnowledgeHub.Contracts.Errors;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[ApiController]
[ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResult), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResult), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase() : ControllerBase
{
}

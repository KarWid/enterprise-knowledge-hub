using MediatR;

namespace EnterpriseKnowledgeHub.Modules.Identity.Application.Health;

public sealed record GetHealthQuery : IRequest<GetHealthResult>;

using MediatR;

namespace EnterpriseKnowledgeHub.Application.Queries.GetCurrentUserOverview
{
    public sealed record GetCurrentUserOverviewQuery() : IRequest<GetCurrentUserOverviewQueryResult>;
}

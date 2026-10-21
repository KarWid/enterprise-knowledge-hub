using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseKnowledgeHub.BuildingBlocks;

public static class BuildingBlocksModule
{
    public static IServiceCollection AddEnterpriseKnowledgeHubBuildingBlocks(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(_ => TimeProvider.System);
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IdentityModule).Assembly));
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.Modules.Organizations;

public static class OrganizationsModule
{
    public static IServiceCollection AddOrganizationsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrganizationsModule).Assembly));
        return services;
    }
}

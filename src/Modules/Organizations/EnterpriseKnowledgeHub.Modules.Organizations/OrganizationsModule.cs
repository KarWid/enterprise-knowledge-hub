using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.Modules.Organizations;

public static class OrganizationsModule
{
    public static IServiceCollection AddOrganizationsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrganizationsModule).Assembly));

        services.AddScoped<IOrganizationAccessService, OrganizationAccessService>();
        services.Decorate<IOrganizationAccessService, CachedOrganizationAccessService>();

        return services;
    }
}

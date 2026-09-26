using EnterpriseKnowledgeHub.BuildingBlocks.Application.Security;
using EnterpriseKnowledgeHub.Modules.Organizations.Application.Authorization.AuthorizationAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EnterpriseKnowledgeHub.Modules.Organizations.Persistence;

public static class OrganizationsModule
{
    public static IServiceCollection AddOrganizationsModule(this IServiceCollection services, IConfiguration configuration, string connectionStringName)
    {
        services.AddScoped<IOrganizationAccessService, OrganizationAccessService>();
        services.Decorate<IOrganizationAccessService, CachedOrganizationAccessService>();
        services.AddDbContext<OrganizationsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(connectionStringName)));

        return services;
    }
}

using EnterpriseKnowledgeHub.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration, string connectionStringName)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IdentityModule).Assembly));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(connectionStringName))); 

        return services;
    }
}
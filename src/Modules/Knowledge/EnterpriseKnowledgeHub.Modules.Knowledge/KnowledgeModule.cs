using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.Modules.Knowledge;

public static class KnowledgeModule
{
    public static IServiceCollection AddKnowledgeModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        services.Configure<DocumentUploadOptions>(
            configuration.GetSection(DocumentUploadOptions.SectionName));
        services.AddDbContext<KnowledgeDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(connectionStringName)));

        return services;
    }
}

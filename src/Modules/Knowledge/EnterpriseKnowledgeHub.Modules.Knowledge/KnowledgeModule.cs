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
        services.AddOptions<DocumentUploadOptions>()
            .Bind(configuration.GetSection(DocumentUploadOptions.SectionName))
            .Validate(o => o.UploadSasLifetimeMinutes is > 0 and <= 15 && o.MaxUploadSizeBytes is > 0 and <= 524288000,
                "Uploads require a 1-15 minute SAS and a positive size limit of at most 500 MiB (single PUT).")
            .ValidateOnStart();
        services.AddOptions<DocumentUploadWorkerOptions>()
            .Bind(configuration.GetSection(DocumentUploadWorkerOptions.SectionName))
            .Validate(o => o.IsValid(), "Invalid document upload worker timing, lease, batch, or retry options.")
            .ValidateOnStart();
        services.AddScoped<IUploadInbox, UploadInbox>();
        services.AddScoped<IDocumentUploadFinalizer, DocumentUploadFinalizer>();
        services.AddScoped<IDocumentUploadReconciler, DocumentUploadReconciler>();
        services.AddDbContext<KnowledgeDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(connectionStringName)));

        return services;
    }
}

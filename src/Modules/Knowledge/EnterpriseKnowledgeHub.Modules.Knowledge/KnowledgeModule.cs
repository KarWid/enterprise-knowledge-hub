using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
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
        AddDocumentUpload(services, configuration);
        AddDocumentTextExtraction(services, configuration);
        AddPersistence(services, configuration, connectionStringName);

        return services;
    }

    private static void AddDocumentUpload(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DocumentUploadOptions>()
            .Bind(configuration.GetSection(DocumentUploadOptions.SectionName))
            .Validate(o => o.UploadSasLifetimeMinutes is > 0 and <= 15 && o.MaxUploadSizeBytes is > 0 and <= 524288000,
                "Uploads require a 1-15 minute SAS and a positive size limit of at most 500 MiB (single PUT).")
            .Validate(o => o.HasValidFormats(), "Documents:EnabledFormats must contain only supported format identifiers: pdf, markdown.")
            .ValidateOnStart();

        services.AddOptions<DocumentUploadWorkerOptions>()
            .Bind(configuration.GetSection(DocumentUploadWorkerOptions.SectionName))
            .Validate(o => o.IsValid(), "Invalid document upload worker timing, lease, batch, or retry options.")
            .ValidateOnStart();

        services.AddScoped<IUploadInbox, UploadInbox>();
        services.AddScoped<IDocumentUploadFinalizer, DocumentUploadFinalizer>();
        services.AddScoped<IDocumentUploadReconciler, DocumentUploadReconciler>();
    }

    private static void AddDocumentTextExtraction(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<DocumentTextExtractorRegistry>();
        services.AddOptions<DocumentTextExtractionOptions>()
            .Bind(configuration.GetSection(DocumentTextExtractionOptions.SectionName))
            .Validate(options => options.IsValid(), "Invalid extraction size limits or format identifiers.")
            .Validate<DocumentTextExtractorRegistry>((options, extractors) => extractors.SupportsEnabledFormats(options),
                "Every enabled extraction format requires a registered, versioned text extractor.")
            .ValidateOnStart();

        services.AddOptions<DocumentTextExtractionWorkerOptions>()
            .Bind(configuration.GetSection(DocumentTextExtractionWorkerOptions.SectionName))
            .Validate(options => options.IsValid(), "Invalid extraction worker timing, lease, batch, or retry options.")
            .ValidateOnStart();

        services.AddScoped<IDocumentTextExtractionScheduler, DocumentTextExtractionScheduler>();
        services.AddScoped<IDocumentTextExtractionQueue, DocumentTextExtractionQueue>();
        services.AddScoped<IDocumentTextExtractionProcessor, DocumentTextExtractionProcessor>();
        services.AddScoped<DocumentTextExtractionStatusResolver>();
    }

    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        services.AddDbContext<KnowledgeDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(connectionStringName)));
    }
}

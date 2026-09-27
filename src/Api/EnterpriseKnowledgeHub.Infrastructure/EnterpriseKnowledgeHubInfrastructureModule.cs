using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseKnowledgeHub.Infrastructure.BlobStorage;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Infrastructure;

public static class EnterpriseKnowledgeHubInfrastructureModule
{
    public static IServiceCollection AddEnterpriseKnowledgeHubInfrastructureModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        services.AddSingleton<BlobServiceClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            if (!Uri.TryCreate(options.AccountUrl, UriKind.Absolute, out var accountUri))
                throw new InvalidOperationException("BlobStorage:AccountUrl must be configured with the storage account blob endpoint.");

            if (string.IsNullOrWhiteSpace(options.ContainerName))
                throw new InvalidOperationException("BlobStorage:ContainerName must be configured.");

            return new BlobServiceClient(
                accountUri,
                serviceProvider.GetRequiredService<TokenCredential>());
        });
        
        services.AddSingleton<BlobContainerClient>(serviceProvider => serviceProvider
            .GetRequiredService<BlobServiceClient>()
            .GetBlobContainerClient(serviceProvider
                .GetRequiredService<IOptions<BlobStorageOptions>>()
                .Value
                .ContainerName));

        services.AddSingleton<IDocumentBlobStorage, AzureBlobDocumentStorage>();

        return services;
    }
}

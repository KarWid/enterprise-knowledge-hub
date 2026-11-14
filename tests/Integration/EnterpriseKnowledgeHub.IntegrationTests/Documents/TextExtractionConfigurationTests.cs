using EnterpriseKnowledgeHub.Infrastructure.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class TextExtractionConfigurationTests
{
    [Theory]
    [InlineData("pdf", "true")]
    [InlineData("unknown", "false")]
    public void UnsupportedOrUnimplementedEnabledFormat_FailsOptionsValidation(string format, string value)
    {
        // Arrange
        var settings = new Dictionary<string, string?> { [$"DocumentTextExtraction:EnabledFormats:{format}"] = value };
        using var services = CreateServices(settings);

        // Act
        var readOptions = () => services.GetRequiredService<IOptions<DocumentTextExtractionOptions>>().Value;

        // Assert
        Assert.Throws<OptionsValidationException>(readOptions);
    }

    [Fact]
    public void DefaultConfiguration_EnablesOnlyMarkdownExtraction()
    {
        // Arrange
        using var services = CreateServices([]);

        // Act
        var options = services.GetRequiredService<IOptions<DocumentTextExtractionOptions>>().Value;

        // Assert
        Assert.True(options.EnabledFormats["markdown"]);
        Assert.False(options.EnabledFormats["pdf"]);
    }

    [Theory]
    [InlineData("DocumentTextExtraction:MaxTextCharacters", "0")]
    [InlineData("DocumentTextExtraction:MaxSourceSizeBytes", "0")]
    [InlineData("DocumentTextExtractionWorker:LeaseSeconds", "90")]
    public void InvalidBounds_FailOptionsValidation(string setting, string value)
    {
        // Arrange
        using var services = CreateServices(new Dictionary<string, string?> { [setting] = value });

        // Act
        Action readOptions = setting.StartsWith("DocumentTextExtractionWorker:", StringComparison.Ordinal)
            ? () => _ = services.GetRequiredService<IOptions<DocumentTextExtractionWorkerOptions>>().Value
            : () => _ = services.GetRequiredService<IOptions<DocumentTextExtractionOptions>>().Value;

        // Assert
        Assert.Throws<OptionsValidationException>(readOptions);
    }

    private static ServiceProvider CreateServices(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddKnowledgeModule(configuration, "test");
        services.AddSingleton<IDocumentTextExtractor, MarkdownDocumentTextExtractor>();
        return services.BuildServiceProvider();
    }
}

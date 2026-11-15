using EnterpriseKnowledgeHub.Infrastructure.Chunking;
using EnterpriseKnowledgeHub.Infrastructure.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class ChunkingConfigurationTests
{
    [Fact]
    public void Defaults_EnableOnlyRegisteredMarkdownChunker()
    {
        // Arrange
        using var services = CreateServices([]);

        // Act
        var options = services.GetRequiredService<IOptions<DocumentChunkingOptions>>().Value;

        // Assert
        Assert.True(options.EnabledFormats["markdown"]);
        Assert.False(options.EnabledFormats["pdf"]);
        Assert.Equal(500, options.TargetTokens);
        services.GetRequiredService<IStartupValidator>().Validate();
    }

    [Theory]
    [InlineData("DocumentChunking:EnabledFormats:pdf", "true")]
    [InlineData("DocumentChunking:EnabledFormats:unknown", "false")]
    [InlineData("DocumentChunking:TargetTokens", "0")]
    [InlineData("DocumentChunking:TargetTokens", "900")]
    [InlineData("DocumentChunking:MaxTokens", "0")]
    [InlineData("DocumentChunking:OverlapTokens", "250")]
    [InlineData("DocumentChunking:MaxChunksPerDocument", "0")]
    [InlineData("DocumentChunking:MaxSourceCharacters", "0")]
    [InlineData("DocumentChunkingWorker:LeaseSeconds", "90")]
    public void InvalidConfiguration_FailsAtStartup(string key, string value)
    {
        // Arrange
        using var services = CreateServices(new() { [key] = value });

        // Act
        var validate = () => services.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        Assert.ThrowsAny<OptionsValidationException>(validate);
    }

    [Fact]
    public void MissingEnabledChunker_FailsEvenWhenWorkerIsDisabled()
    {
        // Arrange
        using var services = CreateServices(new() { ["DocumentChunkingWorker:Enabled"] = "false" }, false);

        // Act
        var validate = () => services.GetRequiredService<IOptions<DocumentChunkingOptions>>().Value;

        // Assert
        Assert.Throws<OptionsValidationException>(validate);
    }

    [Fact]
    public void DisabledFormat_DoesNotRequireAChunker()
    {
        // Arrange
        using var services = CreateServices(new() { ["DocumentChunking:EnabledFormats:markdown"] = "false" }, false);

        // Act
        var options = services.GetRequiredService<IOptions<DocumentChunkingOptions>>().Value;

        // Assert
        Assert.False(options.EnabledFormats["markdown"]);
    }

    private static ServiceProvider CreateServices(Dictionary<string, string?> settings, bool registerChunker = true)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddKnowledgeModule(configuration, "test");
        services.AddSingleton<IDocumentTextExtractor, MarkdownDocumentTextExtractor>();
        if (registerChunker) services.AddSingleton<IDocumentChunker, MarkdownDocumentChunker>();
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UnversionedChunker_FailsConfigurationValidation(string version)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKnowledgeModule(new ConfigurationBuilder().Build(), "test");
        services.AddSingleton<IDocumentChunker>(new UnversionedChunker(version));
        using var provider = services.BuildServiceProvider();

        // Act
        var validate = () => provider.GetRequiredService<IOptions<DocumentChunkingOptions>>().Value;

        // Assert
        Assert.Throws<OptionsValidationException>(validate);
    }

    [Fact]
    public void DuplicateFormatRegistrations_AreRejected()
    {
        // Arrange
        IDocumentChunker[] registrations = [new UnversionedChunker("v1"), new UnversionedChunker("v2")];

        // Act
        var create = () => new DocumentChunkerRegistry(registrations);

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    private sealed class UnversionedChunker(string version) : IDocumentChunker
    {
        public DocumentFormat Format => DocumentFormat.Markdown;
        public string Version => version;
        public IReadOnlyList<DocumentChunkContent> Chunk(string text, string documentName, DocumentChunkingSettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

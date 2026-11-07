using EnterpriseKnowledgeHub.Modules.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents;

public sealed class DocumentUploadOptionsTests
{
    [Fact]
    public void ConfigurationCanDisableOneFormatWithoutDisablingTheOther()
    {
        using var services = CreateServices(new Dictionary<string, string?>
        {
            ["Documents:EnabledFormats:markdown"] = "false"
        });
        
        var options = services.GetRequiredService<IOptions<DocumentUploadOptions>>().Value;

        Assert.True(options.IsEnabled(DocumentFormat.Pdf));
        Assert.False(options.IsEnabled(DocumentFormat.Markdown));
    }

    [Fact]
    public void UnknownConfiguredFormatFailsValidation()
    {
        using var services = CreateServices(new Dictionary<string, string?>
        {
            ["Documents:EnabledFormats:exe"] = "true"
        });

        Assert.Throws<OptionsValidationException>(() =>
            services.GetRequiredService<IOptions<DocumentUploadOptions>>().Value);
    }

    private static ServiceProvider CreateServices(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddKnowledgeModule(configuration, "Test").BuildServiceProvider();
    }
}

using EnterpriseKnowledgeHub.Infrastructure.DocumentUploads.Validation;
using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.IntegrationTests.Documents;

public sealed class DocumentContentValidatorRegistrationTests
{
    [Fact]
    public void EverySupportedFormatHasExactlyOneRegisteredValidator()
    {
        using var factory = new StorageWebhookFactory();
        var validators = factory.Services.GetServices<IDocumentContentValidator>().ToArray();

        foreach (var format in DocumentFormat.All)
        {
            var matchingValidators = validators.Where(validator => validator.Format == format).ToArray();

            Assert.True(
                matchingValidators.Length == 1,
                $"Format '{format.Id}' must have exactly one registered content validator; found {matchingValidators.Length}.");
        }
    }
}

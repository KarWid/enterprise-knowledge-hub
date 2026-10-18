using EnterpriseKnowledgeHub.BuildingBlocks.Application;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Application.Documents;

public sealed class DurableWorkerOptionsTests
{
    [Fact]
    public void CommonDefaultsAreValid()
    {
        Assert.True(new DurableWorkerOptions().IsValid());
    }

    [Fact]
    public void LeaseMustOutliveProcessingTimeoutBySafetyMargin()
    {
        var options = new DurableWorkerOptions
        {
            ProcessingTimeoutSeconds = 90,
            LeaseSeconds = 104
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void RetryMaximumCannotBeShorterThanRetryBase()
    {
        var options = new DurableWorkerOptions
        {
            RetryBaseSeconds = 60,
            RetryMaxSeconds = 59
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void UploadOptionsIncludeCommonWorkerValidation()
    {
        var options = new DocumentUploadWorkerOptions { BatchSize = 0 };

        Assert.False(options.IsValid());
    }

    [Theory]
    [InlineData(59)]
    [InlineData(86401)]
    public void ReservationGraceMustStayWithinUploadLimits(int seconds)
    {
        var options = new DocumentUploadWorkerOptions { ReservationGraceSeconds = seconds };

        Assert.False(options.IsValid());
    }
}

using EnterpriseKnowledgeHub.BuildingBlocks;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseKnowledgeHub.UnitTests;

public sealed class BuildingBlocksModuleTests
{
    [Fact]
    public void RegistersSystemTimeProviderByDefault()
    {
        var services = new ServiceCollection();

        services.AddEnterpriseKnowledgeHubBuildingBlocks();

        using var provider = services.BuildServiceProvider();
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void PreservesTimeProviderRegisteredByHost()
    {
        var clock = new TestTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);

        services.AddEnterpriseKnowledgeHubBuildingBlocks();

        using var provider = services.BuildServiceProvider();
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
    }

    private sealed class TestTimeProvider : TimeProvider
    {
    }
}

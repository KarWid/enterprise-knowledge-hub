namespace EnterpriseKnowledgeHub.BuildingBlocks.Application;

/// <summary>
/// Common execution policy for a polling worker that claims durable, leased work.
/// Feature-specific options should derive from this type and extend validation
/// with settings that belong to that feature.
/// </summary>
public class DurableWorkerOptions
{
    public bool Enabled { get; init; } = true;
    public int PollSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 10;
    public int MaxAttempts { get; init; } = 8;
    public int RetryBaseSeconds { get; init; } = 10;
    public int RetryMaxSeconds { get; init; } = 600;
    public int LeaseSeconds { get; init; } = 120;
    public int ProcessingTimeoutSeconds { get; init; } = 90;

    public virtual bool IsValid() =>
        PollSeconds is >= 1 and <= 300 &&
        BatchSize is >= 1 and <= 100 &&
        MaxAttempts is >= 1 and <= 20 &&
        RetryBaseSeconds > 0 &&
        RetryMaxSeconds >= RetryBaseSeconds &&
        RetryMaxSeconds <= 86400 &&
        ProcessingTimeoutSeconds is >= 1 and <= 600 &&
        LeaseSeconds >= ProcessingTimeoutSeconds + 15 &&
        LeaseSeconds <= 3600;
}

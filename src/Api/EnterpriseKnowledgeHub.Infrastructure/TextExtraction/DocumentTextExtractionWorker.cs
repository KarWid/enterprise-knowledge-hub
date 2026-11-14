using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Infrastructure.TextExtraction;

public sealed class DocumentTextExtractionWorker(
    IServiceScopeFactory scopes,
    IOptions<DocumentTextExtractionWorkerOptions> options,
    ILogger<DocumentTextExtractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // Dependency exceptions can include document URLs; log only stable diagnostics.
                logger.LogError("Document text extraction could not access its dependencies; retrying next poll.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task RunBatchAsync(CancellationToken cancellationToken)
    {
        await using (var scope = scopes.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDocumentTextExtractionScheduler>().ScheduleAsync(cancellationToken);
        }

        for (var index = 0; index < options.Value.BatchSize; index++)
        {
            await using var scope = scopes.CreateAsyncScope();
            var queue = scope.ServiceProvider.GetRequiredService<IDocumentTextExtractionQueue>();
            var work = await queue.ClaimAsync(cancellationToken);
            if (work is null)
            {
                return;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.ProcessingTimeoutSeconds));

            try
            {
                if (work.Attempts > options.Value.MaxAttempts)
                {
                    await queue.RetryAsync(work, DocumentTextExtractionErrorCodes.LeaseRecoveryExhausted, cancellationToken);
                    continue;
                }

                await scope.ServiceProvider.GetRequiredService<IDocumentTextExtractionProcessor>().ProcessAsync(work, timeout.Token);
                logger.LogInformation("Extraction work {WorkId} document {DocumentId} organization {OrganizationId} finished attempt {Attempt}.",
                    work.Id, work.DocumentId, work.OrganizationId, work.Attempts);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutdown leaves the lease recoverable by another process after expiry.
                throw;
            }
            catch (DocumentTextExtractionException exception)
            {
                await queue.FailAsync(work, exception.ErrorCode, cancellationToken);
                logger.LogWarning("Extraction work {WorkId} organization {OrganizationId} failed ({ErrorCode}).",
                    work.Id, work.OrganizationId, exception.ErrorCode);
            }
            catch (Exception exception)
            {
                var errorCode = exception is OperationCanceledException
                    ? DocumentTextExtractionErrorCodes.ProcessingTimeout
                    : DocumentTextExtractionErrorCodes.DependencyFailure;
                    
                await queue.RetryAsync(work, errorCode, cancellationToken);
                logger.LogWarning("Extraction work {WorkId} organization {OrganizationId} attempt {Attempt} failed ({ErrorCode}); exhausted: {Exhausted}.",
                    work.Id, work.OrganizationId, work.Attempts, errorCode, work.Attempts >= options.Value.MaxAttempts);
            }
        }
    }
}

using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseKnowledgeHub.Infrastructure.DocumentUploads;

public sealed class DocumentUploadWorker(
    IServiceScopeFactory scopes, 
    IOptions<DocumentUploadWorkerOptions> options,
    ILogger<DocumentUploadWorker> logger) : BackgroundService
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception)
            {
                // Exceptions from Storage may include request URLs. Never log their messages or payloads.
                logger.LogError("Document upload worker could not access its dependencies; retrying next poll.");
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

    public async Task RunBatchAsync(CancellationToken ct)
    {
        await using (var scope = scopes.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDocumentUploadReconciler>().ScheduleAsync(ct);
        }

        // One item at a time per instance. Claim immediately before processing, never pre-lease a batch.
        for (var i = 0; i < options.Value.BatchSize; i++)
        {
            await using var scope = scopes.CreateAsyncScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IUploadInbox>();
            var work = await inbox.ClaimAsync(ct);
            if (work is null)
            {
                return;  
            } 

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.ProcessingTimeoutSeconds));
            
            try
            {
                if (work.Attempts > options.Value.MaxAttempts)
                {
                    await inbox.RetryAsync(work, "lease_recovery_exhausted", ct);
                    logger.LogError("Upload work {WorkId} event {EventId} exhausted its lease recoveries.", work.Id, work.EventId);
                    continue;
                }
                
                await scope.ServiceProvider.GetRequiredService<IDocumentUploadFinalizer>().ProcessAsync(work, timeout.Token);
                await inbox.CompleteAsync(work, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Leave the lease intact; it will expire after shutdown/crash.
                throw;
            }
            catch (Exception exception)
            {
                var error = exception switch
                {
                    DocumentUploadDeferredException deferred => deferred.ErrorCode,
                    DbUpdateConcurrencyException => "document_concurrency",
                    OperationCanceledException => "processing_timeout",
                    _ => "dependency_failure"
                };

                await inbox.RetryAsync(work, error, ct);
                
                logger.LogWarning("Upload work {WorkId} event {EventId} attempt {Attempt} failed ({ErrorCode}); exhausted: {Exhausted}.",
                    work.Id, work.EventId, work.Attempts, error, work.Attempts >= options.Value.MaxAttempts);
            }
        }
    }
}

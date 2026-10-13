using System.Text.Json;
using EnterpriseKnowledgeHub.Api.StorageEvents;
using EnterpriseKnowledgeHub.Modules.Knowledge.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("webhooks/storage-events")]
[Authorize(Policy = StorageEventAuthentication.Policy)]
public sealed class StorageEventsController(
    StorageEventParser parser, 
    IUploadInbox inbox,
    ILogger<StorageEventsController> logger) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> Receive([FromBody] JsonElement events, CancellationToken ct)
    {
        ParsedStorageEvents parsed;
        try 
        { 
            parsed = parser.Parse(events); 
        }
        catch (ArgumentException) 
        { 
            return BadRequest(); 
        }

        if (parsed.ValidationCode is not null)
        {
            return Ok(new { validationResponse = parsed.ValidationCode });
        }
        
        try
        {
            await inbox.EnqueueAsync(parsed.Items, ct);
            return Ok(); // Every item is committed or confirmed duplicate before acknowledgment.
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            logger.LogError("Storage event batch could not be persisted; requesting delivery retry.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}

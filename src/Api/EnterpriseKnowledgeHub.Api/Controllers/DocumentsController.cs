using EnterpriseKnowledgeHub.Api.Authorization;
using EnterpriseKnowledgeHub.Api.Mappers;
using EnterpriseKnowledgeHub.Contracts.Knowledge;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.BeginDocumentUpload;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocument;
using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocuments;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseKnowledgeHub.Api.Controllers;

[Route("api/documents")]
[Authorize]
public sealed class DocumentsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Documents.Read)]
    [SwaggerOperation(OperationId = "GetDocuments")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var documents = await mediator.Send(new GetDocumentsQuery(), cancellationToken);

        return Ok(DocumentMapper.MapToDocumentResponses(documents));
    }

    [HttpPost("uploads")]
    [Authorize(Policy = Policies.Documents.Upload)]
    [SwaggerOperation(OperationId = "BeginDocumentUpload")]
    [ProducesResponseType(typeof(DocumentUploadSessionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> BeginUpload(
        [FromBody] CreateDocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new BeginDocumentUploadCommand(request.FileName),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            DocumentMapper.MapToDocumentUploadSessionResponse(result));
    }

    [HttpGet("{documentId:guid}")]
    [Authorize(Policy = Policies.Documents.Read)]
    [SwaggerOperation(OperationId = "GetDocument")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDocumentQuery(documentId),
            cancellationToken);

        return result is null ? NotFound() : Ok(DocumentMapper.MapToDocumentResponse(result));
    }
}

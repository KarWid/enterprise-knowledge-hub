using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.GetDocumentUploadCapabilities;

internal sealed record GetDocumentUploadCapabilitiesQuery : IRequest<DocumentUploadCapabilitiesResult>;

public sealed record DocumentUploadCapabilitiesResult(IReadOnlyList<DocumentUploadFormatResult> Formats, long MaxSizeBytes);

public sealed record DocumentUploadFormatResult(string Id, string Extension, string ContentType);

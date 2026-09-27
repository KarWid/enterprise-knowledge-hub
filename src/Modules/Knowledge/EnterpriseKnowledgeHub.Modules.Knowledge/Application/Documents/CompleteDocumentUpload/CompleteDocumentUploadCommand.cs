using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.CompleteDocumentUpload;

internal sealed record CompleteDocumentUploadCommand(Guid DocumentId) : IRequest<DocumentResult>;

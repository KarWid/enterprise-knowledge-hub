using Mediator;

namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.BeginDocumentUpload;

internal sealed record BeginDocumentUploadCommand(string FileName) : IRequest<DocumentUploadSessionResult>;

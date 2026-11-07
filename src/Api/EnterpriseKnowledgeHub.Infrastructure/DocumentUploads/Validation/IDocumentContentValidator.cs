using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Infrastructure.DocumentUploads.Validation;

internal interface IDocumentContentValidator
{
    DocumentFormat Format { get; }

    // Null means the entire file is needed, within the upload size limit already checked by storage.
    long? MaximumInspectionBytes { get; }

    Task<bool> ValidateAsync(Stream content, long contentLength, CancellationToken cancellationToken);
}

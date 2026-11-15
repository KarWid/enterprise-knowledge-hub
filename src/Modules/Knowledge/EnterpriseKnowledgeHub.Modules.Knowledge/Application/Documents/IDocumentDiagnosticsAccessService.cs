namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public interface IDocumentDiagnosticsAccessService
{
    ValueTask<bool> CanViewDiagnosticsAsync(CancellationToken cancellationToken);
}

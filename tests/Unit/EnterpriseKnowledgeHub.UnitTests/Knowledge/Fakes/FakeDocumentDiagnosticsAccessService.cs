using EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

namespace EnterpriseKnowledgeHub.UnitTests.Knowledge.Fakes;

internal sealed class FakeDocumentDiagnosticsAccessService(bool canViewDiagnostics = true)
    : IDocumentDiagnosticsAccessService
{
    public ValueTask<bool> CanViewDiagnosticsAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(canViewDiagnostics);
}

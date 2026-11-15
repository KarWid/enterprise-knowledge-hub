namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents;

public sealed class DocumentDiagnosticsOptions
{
    public const string SectionName = "DocumentDiagnostics";

    // Availability only; existing authorization and Organization checks still apply.
    public bool Enabled { get; init; }
}

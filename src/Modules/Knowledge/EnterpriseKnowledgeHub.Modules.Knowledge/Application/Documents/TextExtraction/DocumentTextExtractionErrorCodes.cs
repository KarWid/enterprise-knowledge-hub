namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.TextExtraction;

public static class DocumentTextExtractionErrorCodes
{
    public const string EmptyText = "empty_text";
    public const string InvalidUtf8 = "invalid_utf8";
    public const string InvalidTextContent = "invalid_text_content";
    public const string TextLimitExceeded = "text_limit_exceeded";
    public const string SourceMissing = "source_missing";
    public const string SourceSizeLimit = "source_size_limit";
    public const string SourceETagChanged = "source_etag_changed";
    public const string SourceNotEligible = "source_not_eligible";
    public const string SourceIdentityMismatch = "source_identity_mismatch";
    public const string SourceChangedDuringExtraction = "source_changed_during_extraction";
    public const string ExtractorVersionUnavailable = "extractor_version_unavailable";
    public const string ProcessingTimeout = "processing_timeout";
    public const string DependencyFailure = "dependency_failure";
    public const string LeaseRecoveryExhausted = "lease_recovery_exhausted";
}

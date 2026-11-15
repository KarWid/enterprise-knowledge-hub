namespace EnterpriseKnowledgeHub.Modules.Knowledge.Application.Documents.Chunking;

public static class DocumentChunkingErrorCodes
{
    public const string SourceNotEligible = "chunk_source_not_eligible";
    public const string ChunkerVersionUnavailable = "chunker_version_unavailable";
    public const string InvalidContent = "chunk_invalid_content";
    public const string SourceLimitExceeded = "chunk_source_limit";
    public const string ChunkLimitExceeded = "chunk_count_limit";
    public const string InvalidChunks = "chunk_invalid_output";
    public const string DependencyFailure = "chunk_dependency_failure";
    public const string ProcessingTimeout = "chunk_processing_timeout";
    public const string LeaseRecoveryExhausted = "chunk_lease_recovery_exhausted";
}

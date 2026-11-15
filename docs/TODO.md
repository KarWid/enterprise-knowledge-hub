# TODO

- [x] Protect document diagnostics with `DocumentDiagnostics:Enabled` and the `Documents.Diagnostics` policy. All four document query handlers use the shared access service. Diagnostics require an authenticated `OrganizationOwner` in the validated current Organization; other readable roles receive basic document responses and cannot access text/chunk previews. Extraction and chunking continue independently of diagnostics access.

- [ ] Hide all extracted-text preview controls when diagnostics access is denied. Chunk-preview controls already follow the optional chunking metadata. The backend remains authoritative and denies direct preview requests; preserve Organization isolation when exposing any UI capability information.

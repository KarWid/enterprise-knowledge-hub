# TODO

- [x] Protect document diagnostics with `DocumentDiagnostics:Enabled` and the `Documents.Diagnostics` policy. All four document query handlers use the shared access service. Diagnostics require an authenticated `OrganizationOwner` in the validated current Organization; other readable roles receive basic document responses and cannot access text/chunk previews. Extraction and chunking continue independently of diagnostics access.

- [x] Hide document preview actions when diagnostics access is denied. The action menu uses the optional chunking metadata as the current full diagnostics view marker. Content preview additionally requires completed extraction; chunk preview requires completed chunking. Backend policy checks remain authoritative.

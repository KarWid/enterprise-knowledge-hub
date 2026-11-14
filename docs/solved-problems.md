# Solved problems

Notes for discussing engineering decisions and obstacles in interviews.

## Organization isolation in RTK Query caching

### Context and risk

Users can belong to multiple Organizations and switch the active Organization.
The API receives the selected Organization through the `X-Organization-Id` header
and validates access on the backend.

Originally, shared request preparation supplied this header from Redux state.
However, RTK Query identifies cached data by endpoint name and serialized query
arguments. Headers added in `prepareHeaders` do not automatically affect that
identity. Two calls to `getDocuments()` could therefore share a cache entry even
when their requests target different Organizations. This could display a previous
Organization's cached documents after switching Organizations; it is a frontend
cache isolation risk, not evidence that backend authorization was bypassed.

### Implemented solution

- Include `X-Organization-Id` explicitly in document query arguments. This creates
  separate cache entries for each Organization and makes Organization selection
  changes update query subscriptions.
- Let generated endpoints send that same argument as the HTTP header.
- Preserve explicitly supplied Organization headers in `requestHeaders.ts`;
  populate the header from Redux only when no explicit header exists.
- Use `currentData` when displaying Organization-owned query results so data
  from previous hook arguments is not displayed while new arguments load.
- Skip Organization-owned queries when no Organization is selected.

Preserving the header also prevents an asynchronous race: a request starts for
Organization A, token acquisition awaits, and the user switches to B. Overwriting
the header with B at that point could put B's response into A's cache entry.
The request header must stay consistent with the Organization in its query args,
including during retries.

### Interview explanation

"I identified that selecting the correct Organization for HTTP requests was not
enough: our frontend cache also needed that Organization in its identity. I added
it to query arguments, preserved the corresponding header across asynchronous
token acquisition, and used current-argument data in the UI. Backend membership
checks remained the authoritative access boundary."

### Possible follow-up: shared Organization-aware API hooks

The current solution requires call sites to supply the Organization argument.
A shared API facade could read the selected Organization, add it to generated
query arguments, skip requests without a selection, and expose `currentData`.
Components could then call `useOrganizationDocumentsQuery()` without knowing
the HTTP header name. Mutation wrappers should capture the Organization when
the operation starts and preserve it for subsequent steps of that operation.

This is a proposed refinement, not an implemented change. Adding the header
only in `baseQuery` would still be too late to change RTK Query's cache identity.
Avoid a serializer that reads mutable global store state: cache identity should
be derived consistently from the arguments of each subscription and request.

References:

- [RTK Query cache behavior](https://redux.js.org/toolkit/rtk-query/usage/cache-behavior)
- [RTK Query createApi and serializeQueryArgs](https://redux.js.org/toolkit/rtk-query/api/createApi)

Relevant code:

- `src/Web/src/services/api/requestHeaders.ts`
- `src/Web/src/features/documents/services/useDocumentUpload.ts`
- `src/Web/src/features/documents/services/useDocumentsPage.ts`

Enterprise Knowledge Hub — Security
1. Security Principles
Security is a first-class architectural concern.
The application follows these principles:
Least privilege
Defense in depth
Tenant isolation
Application-owned authorization
Identity provider separation
Secure-by-default Azure configuration
No secrets in source control
Managed identities wherever supported
Minimal exposure of customer data
Authorization before data retrieval
Security decisions should favor well-established platform capabilities over custom security mechanisms.

2. Identity and Authentication
2.1 Identity Provider
Microsoft Entra / Microsoft Entra External ID is the primary identity platform.
The application should not implement its own password storage or authentication protocol.
Authentication is delegated to the configured identity provider.
The identity provider is responsible for answering:
Who is this user?
The application is responsible for answering:
Which organization does this user belong to?
and:
What can this user do?

2.2 Authentication Flow
The frontend uses MSAL to authenticate users.
Conceptually:
User
  ↓
React
  ↓
MSAL
  ↓
Microsoft Entra / External ID
  ↓
Access Token
  ↓
ASP.NET Core API

The API validates the access token before processing authenticated requests.
The frontend must never be trusted to establish identity or permissions.

2.3 External Identities
The application should not assume that every customer has their own Microsoft Entra tenant.
Customers may use:
Microsoft Entra,
Microsoft Entra External ID,
other supported identity providers in the future.
The domain model remains independent of the identity provider.

3. Application Identity
The application maintains its own ApplicationUser.
Example:
ApplicationUser
----------------
Id
ExternalIdentityId
Email
DisplayName
CreatedAt
Status

The external identity identifier is used to associate the authenticated identity with an application user.
The application should not copy unnecessary identity-provider data into the domain.
Identity-provider-specific claims should remain at the infrastructure/application boundary.

4. Organizations and Multi-Tenancy
4.1 Organization Is the Tenant Boundary
Organization is the application's tenant boundary.
It is not defined by Microsoft Entra.
Identity
   ↓
ApplicationUser
   ↓
Membership
   ↓
Organization

All organization-owned data must belong to exactly one Organization.

4.2 Organization Isolation
Tenant isolation is mandatory.
An authenticated user must never be able to access another organization's data by modifying:
IDs,
URLs,
request bodies,
query parameters,
route parameters,
search queries,
document identifiers,
conversation identifiers.
The backend must establish the current organization context from authenticated application state.
The client must not be allowed to arbitrarily select an OrganizationId and thereby gain access to that organization.

4.3 Organization Context
Every request operating on tenant-owned resources must execute within an explicit Organization context.
Conceptually:
Authenticated Identity
        ↓
ApplicationUser
        ↓
Membership
        ↓
Current Organization
        ↓
Application Operation

The organization context must be validated before accessing tenant-owned data.

5. Membership and Authorization
5.1 Membership
A user belongs to an organization through Membership.
ApplicationUser
       │
       ▼
Membership
       │
       ▼
Organization

Membership contains the user's organization-specific role.
Example:
Membership
----------------
UserId
OrganizationId
Role
Status
CreatedAt

A user may belong to multiple organizations.

5.2 Business Roles
Initial roles:
OrganizationOwner
OrganizationAdmin
KnowledgeManager
Employee

Roles are application-domain concepts.
They are not assumed to be Microsoft Entra groups.

6. Policy-Based Authorization
Authorization uses ASP.NET Core policy-based authorization.
Endpoints should express required permissions through policies.
Example:
[Authorize(Policy = Policies.Documents.Upload)]

Avoid authorization logic such as:
if (user.Role == "Admin")
{
    ...
}

inside controllers or application endpoints.
Instead:
Role
  ↓
Permission
  ↓
Authorization Policy
  ↓
Endpoint

Policies should represent application capabilities.
Examples:
Documents.Upload
Documents.Delete
Documents.Read

Users.Invite
Users.Remove

Organization.Read
Organization.Manage

Knowledge.Search
Chat.Use


7. Organization Onboarding
The first authenticated user may create an Organization.
The flow is:
Authenticate
    ↓
No ApplicationUser?
    ↓
Create ApplicationUser
    ↓
Create Organization
    ↓
Create Membership
    ↓
Role = OrganizationOwner

Organization creation is an application operation.
The product operator should not need to manually provision an Azure resource or Microsoft Entra tenant for each customer.

8. Invitations
Organization Owners and authorized administrators may invite users.
The invitation flow is conceptually:
Organization Admin
       ↓
Create Invitation
       ↓
Invitation sent
       ↓
User authenticates
       ↓
Invitation validated
       ↓
ApplicationUser created/found
       ↓
Membership created

The invitation must be associated with exactly one Organization.
An invitation must not allow a user to select or modify the target Organization.
Invitation tokens should:
be cryptographically random,
be short-lived,
be single-use,
not contain sensitive information,
be invalidated after successful use.

9. Tenant-Owned Data
The following data is organization-owned:
Documents
Knowledge
Conversations
Chat messages
Organization memberships
Organization configuration
Other future customer data
Each operation must enforce organization ownership.
Example:
GET /api/documents/{documentId}

        ↓

Resolve current Organization
        ↓
Query Document
WHERE
    Id = documentId
    AND OrganizationId = currentOrganizationId

Never:
SELECT *
FROM Documents
WHERE Id = @documentId

without validating organization ownership.

10. Database Security
The primary database is Azure SQL Database.
The application uses Entity Framework Core.
Database access should follow least privilege.
The application should not use a highly privileged database account for normal application operations.
Where supported and practical, Azure identity-based authentication should be preferred over long-lived SQL credentials.
Connection information and secrets must not be stored in source control.

11. Blob Storage Security
Azure Blob Storage stores original customer documents.
Public access is disabled.
The application must not expose customer documents through publicly accessible blob URLs.
Preferred access pattern:
React
  ↓
ASP.NET Core API
  ↓
Managed Identity
  ↓
Blob Storage

The frontend must not receive storage account keys.
Blob containers should not be publicly readable.
Access to blobs must be scoped to the current Organization and authorized operation.

12. Azure Managed Identity
Managed Identity is the preferred authentication mechanism for Azure-to-Azure communication.
Where supported:
App Service
    ↓
Managed Identity
    ↓
Azure Service

This should be preferred over:
embedded credentials,
connection strings,
long-lived API keys,
credentials committed to configuration files.
Managed identities should receive only the permissions required for their specific responsibilities.

13. Azure Key Vault
Azure Key Vault stores secrets that cannot be eliminated through managed identity.
Examples may include:
third-party credentials,
configuration secrets,
temporary integration secrets.
Applications access Key Vault using Managed Identity.
Secrets must never be:
committed to Git,
included in frontend bundles,
logged,
returned by APIs.

14. Azure AI Search Security
Azure AI Search contains searchable representations of customer knowledge.
Indexed documents/chunks must contain sufficient tenant metadata to enforce organization-level filtering.
Example:
Chunk
----------------
OrganizationId
DocumentId
ChunkId
Content
Embedding

Every retrieval operation must apply the current Organization filter.
Conceptually:
User Question
      ↓
Current Organization
      ↓
AI Search
      ↓
Organization filter
      ↓
Relevant chunks

The application must never perform an unrestricted cross-tenant search.

15. AI Security
AI services must not be responsible for authorization.
Authorization occurs before information is passed to the AI model.
The safe flow is:
User
 ↓
Authentication
 ↓
Authorization
 ↓
Organization Context
 ↓
Tenant-filtered Retrieval
 ↓
Retrieved Knowledge
 ↓
Azure OpenAI
 ↓
Response

Not:
User
 ↓
Azure OpenAI
 ↓
"Please don't reveal data from other organizations."

Prompt instructions are not a security boundary.

16. AI Data Privacy
The application should minimize the amount of customer data sent to AI services.
Only information required to answer the current request should be included in the model context.
The application should not send:
unrelated documents,
unrelated organizations' data,
unnecessary personal information,
internal authorization metadata.
The system should use Azure OpenAI rather than sending customer knowledge to arbitrary public AI APIs.
AI-related services must be configured according to the organization's required Azure security and privacy posture.

17. Document Processing Security
Document processing is performed on behalf of a specific Organization.
The organization context must be preserved throughout the processing pipeline.
Upload
  ↓
Document
  ↓
Processing
  ↓
Extraction
  ↓
Chunking
  ↓
Embedding
  ↓
AI Search

At every stage, the system must retain the document's Organization ownership.
A processing failure must not cause data to be associated with another organization.

18. API Security
The API must:
validate authentication tokens,
enforce authorization policies,
validate all input,
enforce organization boundaries,
avoid exposing internal implementation details,
return appropriate HTTP status codes,
avoid leaking sensitive information in errors.
The API must not trust:
OrganizationId supplied by the client,
UserId supplied by the client,
Role supplied by the client,
permission claims generated by the frontend.

19. Frontend Security
The frontend is considered untrusted.
The frontend may:
display UI based on permissions,
hide unavailable actions,
initiate authentication,
call the API.
The frontend must not be responsible for enforcing authorization.
Example:
Frontend:
"Hide Delete button"

Backend:
"User is not authorized to delete"

The backend decision is authoritative.

20. Secrets Management
Secrets must never be committed to source control.
Forbidden examples:
appsettings.json
.env
source code
Dockerfile
Terraform/Bicep variables
logs

containing real credentials.
Local development should use appropriate developer-specific secret mechanisms.
Production secrets should be managed through Azure-native mechanisms.

21. Logging and Observability
Security-relevant events should be observable.
Examples:
authentication failures,
authorization failures,
invitation creation,
invitation acceptance,
organization creation,
user membership changes,
document access failures,
document processing failures,
unexpected AI errors.
Logs must not contain:
passwords,
access tokens,
refresh tokens,
API keys,
storage keys,
unnecessary document contents,
unnecessary chat contents.

22. Error Handling
Security-sensitive failures should not reveal unnecessary information.
For example, avoid responses such as:
"This document exists in another organization."

Prefer a response that does not reveal cross-tenant information.
The API should generally behave as though an inaccessible resource does not exist.

23. Authorization Order
For tenant-owned operations, the preferred conceptual order is:
1. Authenticate
       ↓
2. Resolve ApplicationUser
       ↓
3. Resolve Membership / Organization Context
       ↓
4. Authorize Operation
       ↓
5. Query Tenant-Scoped Data
       ↓
6. Execute Operation

The system should not retrieve sensitive tenant data before authorization.

24. Security Boundaries
The following boundaries must remain explicit:
Identity Provider
       │
       │ Authentication
       ▼
Application
       │
       │ Authorization
       ▼
Organization
       │
       │ Tenant-scoped data
       ▼
Azure Services

Azure services do not replace application authorization.
AI models do not replace application authorization.
The frontend does not replace application authorization.

25. MVP Security Scope
The MVP must provide:
Microsoft Entra / External ID authentication,
application users,
organization membership,
organization isolation,
policy-based authorization,
secure invitations,
Azure SQL security,
Blob Storage with public access disabled,
Managed Identity where supported,
Key Vault for required secrets,
organization-filtered AI Search,
secure Azure OpenAI integration,
security-aware logging.
The MVP does not require:
microservice-level identity,
service mesh,
complex zero-trust networking,
customer-managed encryption keys,
separate databases per organization,
private endpoints for every Azure service,
advanced SIEM integration,
custom identity infrastructure.
These may be introduced later based on actual security, compliance, or scale requirements.

26. Security Evolution
Security requirements will evolve as the product grows.
Future considerations may include:
SSO policies,
enterprise federation,
customer-managed keys,
Private Endpoints,
network isolation,
advanced auditing,
conditional access integration,
compliance certifications,
data residency,
dedicated tenant infrastructure.
These capabilities should not be implemented prematurely.
The MVP should establish strong foundational security without creating unnecessary infrastructure complexity.


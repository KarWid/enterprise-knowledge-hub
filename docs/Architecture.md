Enterprise Knowledge Hub — Architecture
1. Architectural Overview
Enterprise Knowledge Hub is a multi-tenant SaaS application implemented initially as a modular monolith.
The architecture separates:
Identity
Application / API / API Contract
Domain
Persistence
Knowledge processing
AI / Retrieval
Azure infrastructure
The system is designed to keep the first implementation simple while providing clear boundaries for future growth.

2. High-Level Architecture
                        Users
                           │
                           ▼
                  Microsoft Entra
                 / Entra External ID
                           │
                           │ Authentication
                           ▼
                    React Frontend
                           │
                           │ HTTPS / REST
                           ▼
                    ASP.NET Core API
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
          Identity      Modules      Infrastructure
              │            │            │
              │            │            ├── Azure SQL
              │            │            ├── Blob Storage
              │            │            ├── AI Search
              │            │            ├── Azure OpenAI
              │            │            └── Key Vault
              │            │
              └────────────┘


3. Multi-Tenancy
The application is multi-tenant.
The primary tenant boundary is the application-level Organization.
An Organization is not synonymous with a Microsoft Entra tenant.
An organization may authenticate users through:
Microsoft Entra,
Microsoft Entra External ID,
other supported identity mechanisms in the future.
The application owns the relationship between users and organizations.
Identity Provider
       │
       ▼
Application User
       │
       ▼
Membership
       │
       ▼
Organization

All tenant-owned business data must be associated with an Organization.

4. Organization Isolation
Organization isolation is enforced at the application and persistence layers.
Tenant-owned entities must contain an organization boundary, directly or through their aggregate ownership.
Example:
Organization
    │
    ├── Documents
    ├── Knowledge
    ├── Conversations
    └── Memberships

A request must never be allowed to access another organization's data simply by manipulating an identifier.
Tenant context must be established from the authenticated application identity and validated before accessing tenant-owned resources.

5. Identity Architecture
Identity and authorization are separate concerns.
Identity Provider
Microsoft Entra / Microsoft Entra External ID provides authentication.
The application receives a trusted identity from the configured identity provider.
Application Identity
The application maintains its own representation of a user:
ApplicationUser
----------------
Id
ExternalIdentityId
Email
DisplayName
CreatedAt

The external identity identifier should be treated as provider-specific.
The domain must not depend on Microsoft Entra-specific concepts for basic business behavior.

6. Organization Membership
Users belong to organizations through Membership.
ApplicationUser
       │
       │
       ▼
Membership
       │
       ▼
Organization

This allows the same user to belong to multiple organizations if required in the future.
Membership contains business authorization information such as the user's organization role.
Example:
Membership
----------------
UserId
OrganizationId
Role
CreatedAt


7. Authorization
Authorization uses ASP.NET Core policy-based authorization.
Controllers/endpoints should depend on policies rather than hard-coded role checks.
Example:
[Authorize(Policy = Policies.Documents.Upload)]

Business roles are owned by the application.
Initial roles may include:
OrganizationOwner
OrganizationAdmin
Employee
KnowledgeManager
Roles should map to application permissions through authorization policies.

8. Frontend
The frontend is implemented using:
React
TypeScript
Vite
MSAL
Responsibilities include:
user interface,
authentication flow,
organization onboarding,
organization selection where necessary,
chat interface,
document management,
administration UI.
The frontend must not be trusted for authorization.
All authorization decisions are enforced by the backend.

9. Backend
The backend is an ASP.NET Core REST API.
The application starts as a modular monolith.
Recommended conceptual structure:
src/
  Api/
  Api.Contract/
  Application/
  Domain/
  Infrastructure/
  Modules/

Modules should own their domain logic and application use cases.
Infrastructure concerns should remain outside the domain layer.

10. Persistence
The primary relational database is:
Azure SQL Database
Entity Framework Core is used for persistence.
The database is shared between organizations.
The default tenancy model is:
Single database
    ↓
Shared schema
    ↓
OrganizationId on tenant-owned data

Database-per-tenant is not required for the MVP.

11. Blob Storage
Azure Blob Storage stores original uploaded documents.
Public access to Blob Storage is disabled.
The frontend must not directly access blobs using storage account keys.
Initial MVP flow:
React
  ↓
ASP.NET Core API
  ↓
Managed Identity
  ↓
Blob Storage

A future direct-upload flow using short-lived delegated access may be introduced if required for scalability.

12. Knowledge Processing
The MVP supports PDF documents.
The conceptual pipeline is:
PDF Upload
    ↓
Blob Storage
    ↓
Document Processing
    ↓
Text Extraction
    ↓
Chunking
    ↓
Embedding Generation
    ↓
Azure AI Search

The processing pipeline should preserve organization ownership throughout the entire process.

13. Retrieval-Augmented Generation
The chat uses Retrieval-Augmented Generation.
User Question
      ↓
Authorization
      ↓
Organization Context
      ↓
Azure AI Search
      ↓
Relevant Chunks
      ↓
Azure OpenAI
      ↓
Grounded Response
      ↓
Sources / Citations

Retrieval must be organization-aware.
The AI layer must never be allowed to retrieve data belonging to another organization.

14. Azure AI Search
Azure AI Search stores searchable knowledge representations.
The application is responsible for ensuring that indexed documents contain sufficient tenant information to enforce isolation.
Possible indexing metadata:
OrganizationId
DocumentId
DocumentName
ChunkId
Content

Search queries must be filtered by the current Organization context.

15. Azure OpenAI
Azure OpenAI is used for:
embeddings,
answer generation,
other AI capabilities introduced later.
The model is not responsible for authorization.
The application determines which information can be sent to the model.
The AI layer receives only information that has already passed application-level authorization and retrieval filtering.

16. Managed Identity
Azure Managed Identity is the preferred authentication mechanism for Azure-to-Azure communication.
Where supported:
App Service
    ↓
Managed Identity
    ↓
Azure resource

The application should avoid storing long-lived Azure service credentials.

17. Key Vault
Azure Key Vault stores secrets and sensitive configuration that cannot be replaced by managed identity.
Applications should access Key Vault using Managed Identity.
Secrets must not be committed to source control.

18. Observability
The initial platform uses:
Application Insights
Azure Monitor
The application should provide structured logging and sufficient telemetry to diagnose:
authentication failures,
authorization failures,
document processing failures,
AI retrieval failures,
AI generation failures,
unexpected system errors.
Logs must not contain unnecessary customer-sensitive content.

19. Infrastructure as Code
Azure infrastructure is managed through Infrastructure as Code.
Infrastructure should be reproducible across environments.
The initial environments may include:
Development
Test
Production

The exact IaC technology is an implementation decision and should remain independent from the domain model.

20. Architectural Principles
Domain First
Business rules belong in the domain/application layers rather than controllers or Azure-specific services.
Identity Is Not the Tenant
Microsoft Entra identifies users.
The application owns Organizations and Memberships.
Authorization Is Application-Owned
The application determines what a user can access.
Tenant Isolation Is Mandatory
Every tenant-owned operation must execute within an explicit organization context.
Azure Services Are Infrastructure
Azure SQL, Blob Storage, AI Search, Azure OpenAI, and Key Vault should not leak into the domain model.
Prefer Managed Services
Use Azure managed services instead of implementing infrastructure capabilities ourselves.
Avoid Premature Distribution
The system starts as a modular monolith.
Service extraction should only happen when there is a concrete architectural or operational reason.


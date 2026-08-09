Enterprise Knowledge Hub — Agent Instructions
1. Purpose
This repository contains Enterprise Knowledge Hub, a multi-tenant SaaS application for managing and querying company knowledge.
The application uses:
.NET
ASP.NET Core
Entity Framework Core
React
TypeScript
Vite
Microsoft Entra / External ID
Azure SQL
Azure Blob Storage
Azure AI Search
Azure OpenAI
Azure Key Vault
Azure Managed Identity
Azure Application Insights
The application follows a pragmatic Domain-Driven Design approach and is structured as a modular monolith.

2. Documentation
Do not read all documentation by default.
Read only the documents relevant to the current task.
docs/
├── Vision.md
├── Architecture.md
├── Domain.md
├── AI.md
├── Security.md
└── Development.md

Documentation routing
Task
Read
General product behavior
docs/Vision.md
Architecture / project structure
docs/Architecture.md
Domain model / business rules
docs/Domain.md
AI / RAG
docs/AI.md
Authentication / authorization / security
docs/Security.md
Coding / testing / development workflow
docs/Development.md

When a task crosses multiple areas, read only the relevant documents.
Prefer existing code and local module instructions over unrelated global documentation.

3. Core Architectural Rules
These rules are fundamental.
Organization is the tenant boundary
Organization is the application's tenant.
Do not use Microsoft Entra Tenant ID as the application's tenant identifier.
Users belong to organizations through Membership.
A user may belong to multiple organizations.

Authentication vs Authorization
The identity provider answers:
Who is this user?
The application answers:
Which organization does this user belong to?
and:
What can this user do?
Do not move application authorization into the frontend or AI layer.

Tenant Isolation
All organization-owned data must be organization-scoped.
Never trust an OrganizationId supplied by the frontend as proof of access.
Always establish and validate the organization context on the backend.
A user must never be able to access another organization's data by changing:
IDs,
URLs,
route parameters,
query parameters,
request bodies,
document IDs,
conversation IDs,
search filters.

Policy-Based Authorization
Use ASP.NET Core policy-based authorization.
Prefer authorization policies over direct role checks inside controllers or application services.

AI Is Not a Security Boundary
Azure OpenAI and Azure AI Search must never determine whether a user is authorized to access data.
Authorization and tenant filtering happen before information is sent to the AI model.

Contracts Project
The solution includes a dedicated Contracts project containing Request/Response DTOs and external-facing data models. Contracts do not contain domain logic and do not reference domain entities. They serve as stable communication boundaries between API, modules, workers, and frontend.

4. Architecture
The application is a modular monolith.
Initial modules:
Identity
Organizations
Knowledge
Chat

Modules own their business logic and persistence boundaries.
Do not introduce microservices unless explicitly decided.
Do not create additional modules without a concrete business responsibility.
Do not allow modules to directly manipulate another module's internal entities.
Prefer explicit application-level contracts between modules.

5. DDD
Use Domain-Driven Design pragmatically.
Prefer clear business boundaries over pattern-heavy code.
Use:
Entities
Value Objects
Aggregates
Domain Events
Domain Services
when they provide meaningful value.
Do not introduce abstractions only because they are common in DDD examples.
Avoid speculative:
generic repositories,
generic services,
factories,
base classes,
frameworks,
infrastructure abstractions.

6. Entity Framework Core
EF Core is the persistence technology.
Do not create a generic repository layer over EF Core without a concrete reason.
A module may own its own DbContext.
A module should not casually query another module's tables through its own DbContext.
Database boundaries and module boundaries are related but do not require separate physical databases.

7. Development Approach
Prefer vertical slices.
Build features end-to-end rather than implementing large technical layers in isolation.
The first major milestone is:
React
  ↓
Authentication
  ↓
ASP.NET Core
  ↓
ApplicationUser
  ↓
Organization
  ↓
Membership
  ↓
Azure SQL
  ↓
Organization Dashboard

The application should reach this working state before implementing the RAG pipeline.

8. Work Incrementally
For each task:
Understand the requirement.
Read only relevant documentation.
Inspect the existing code.
Identify affected module(s).
Implement the smallest useful change.
Add or update tests.
Build the solution.
Run relevant tests.
Fix failures.
Summarize the change.
Keep changes small and focused.
Do not modify unrelated code.

9. Do Not Silently Change Architecture
Do not change the following without explicitly explaining the reason:
multi-tenancy model,
identity architecture,
authorization model,
module boundaries,
persistence strategy,
AI architecture,
Azure architecture.
If the current architecture makes a requested feature difficult, explain the conflict and propose an alternative before making a major architectural change.

10. Security
Security is mandatory.
Never:
commit secrets,
expose storage keys,
expose customer documents publicly,
trust frontend authorization,
trust frontend OrganizationId,
bypass authorization for convenience,
perform unrestricted cross-tenant queries,
send unauthorized data to AI services.
Blob Storage public access must remain disabled.
Prefer Managed Identity for Azure-to-Azure authentication.
Use Key Vault for secrets that cannot be eliminated through Managed Identity.

11. Testing
Tests should verify behavior, not implementation details.
Prioritize tests for:
domain rules,
application behavior,
authorization,
tenant isolation,
API behavior,
persistence.
Tenant isolation is security-critical.
Whenever implementing a tenant-owned feature, consider tests proving that Organization A cannot access Organization B's data.
Do not optimize for test coverage percentage alone.

12. Frontend
The frontend uses:
React
TypeScript
Vite
MSAL
RTK Query
The frontend is untrusted.
Frontend authorization is for user experience only.
Backend authorization is authoritative.
Do not duplicate security-sensitive business rules exclusively in React.

13. Azure
Infrastructure is managed through Infrastructure as Code.
Do not introduce Azure resources speculatively.
Add infrastructure when it is required by the current milestone.
Prefer:
Managed Identity

over long-lived credentials where supported.
Do not put Azure-specific implementation details into the domain layer.

14. AI / RAG
AI functionality is currently limited to document-based knowledge.
The initial scope does not include:
querying business databases,
audio processing,
speech-to-text.
The RAG pipeline will eventually be:
PDF
 ↓
Blob Storage
 ↓
Text extraction
 ↓
Chunking
 ↓
Embeddings
 ↓
Azure AI Search
 ↓
Organization-scoped retrieval
 ↓
Azure OpenAI
 ↓
Grounded answer

Every retrieval operation must be scoped to the current Organization.
Never rely on prompts to enforce tenant isolation.

15. Coding Style
Prefer:
clear names,
small methods,
small focused classes,
explicit dependencies,
dependency injection,
asynchronous APIs,
cancellation support,
structured logging,
immutable data where appropriate.
Avoid:
magic strings,
global state,
unnecessary static helpers,
massive service classes,
premature abstractions,
speculative frameworks.
Use terminology defined in Domain.md.
The canonical tenant term is:
Organization

Do not interchange it with:
Tenant
Company
Customer
Workspace

unless discussing an external concept where that terminology is actually appropriate.

16. Definition of Done
A feature is not complete merely because the code compiles.
Before considering a feature complete:
architecture is respected,
domain rules are respected,
authorization is enforced,
tenant isolation is preserved,
relevant tests exist,
build succeeds,
relevant tests pass,
no unnecessary infrastructure was introduced,
no secrets were introduced,
the implementation is understandable.

17. When in Doubt
When several implementations are possible:
Prefer the simplest one.
Prefer the existing project conventions.
Prefer established framework capabilities.
Prefer explicit code over clever abstractions.
Prefer a small working solution over speculative extensibility.
Do not optimize for hypothetical future requirements.
Build what the current milestone actually needs.


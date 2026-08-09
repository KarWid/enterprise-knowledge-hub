Enterprise Knowledge Hub — Development Guide
1. Purpose
This document defines how Enterprise Knowledge Hub should be developed.
It provides development rules and conventions for both human developers and AI coding agents.
The goal is to build the application incrementally while keeping the architecture clean and understandable.
The project should prioritize:
working software,
clear boundaries,
maintainability,
simplicity,
testability,
security,
incremental delivery.
Do not implement infrastructure or abstractions before they are needed.

2. Development Philosophy
2.1 Build Vertically
Features should preferably be implemented as vertical slices.
A vertical slice should connect the necessary layers to produce a working user-visible capability.
Example:
React
  ↓
HTTP API
  ↓
Application
  ↓
Domain
  ↓
EF Core
  ↓
Azure SQL

Avoid implementing entire technical layers in isolation before creating a working feature.

2.2 Prefer Working Software Over Frameworks
Do not create abstractions merely because they might be useful later.
Do not introduce:
generic repositories,
generic services,
excessive base classes,
custom frameworks,
unnecessary factories,
speculative infrastructure,
unless there is a concrete requirement.
Use the simplest implementation that preserves the architectural boundaries.

2.3 Build in Small Milestones
Development should proceed through small, verifiable milestones.
Each milestone should leave the repository in a working state.
A preferred progression is:
1. Repository skeleton
       ↓
2. Application starts
       ↓
3. Frontend starts
       ↓
4. Backend health endpoint
       ↓
5. Database connection
       ↓
6. Authentication
       ↓
7. ApplicationUser
       ↓
8. Organization creation
       ↓
9. Membership / Owner
       ↓
10. Organization dashboard
       ↓
11. User invitations
       ↓
12. Document upload
       ↓
13. Document processing
       ↓
14. AI Search
       ↓
15. RAG chat

Do not jump directly to the final architecture if an intermediate working milestone is possible.

3. First Vertical Slice
The first major goal is not AI.
The first goal is:
A real user can log in, create an organization, become its owner, and see the organization dashboard.
The complete flow should be:
User
  ↓
React
  ↓
Microsoft Entra / External ID
  ↓
ASP.NET Core API
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

When this works end-to-end, the project has a real foundation.
This milestone should be completed before implementing the document/RAG pipeline.

4. First Vertical Slice Requirements
The first vertical slice should support:
Authentication
A user can authenticate using the configured identity provider.
Application User
The authenticated identity is mapped to an ApplicationUser.
Organization Creation
A user without an organization can create one.
Ownership
The user creating the organization automatically receives:
OrganizationOwner

membership.
Dashboard
The user can access an organization dashboard after onboarding.
Tenant Isolation
A user cannot access an organization for which they have no membership.

5. Repository Structure
The initial repository should follow a modular-monolith structure.
enterprise-knowledge-hub/
│
├── src/
│   ├── Api/
│   ├── Contracts/
│   │
│   ├── Web/
│   │
│   ├── Modules/
│   │   ├── Organizations/
│   │   ├── Identity/
│   │   ├── Knowledge/
│   │   └── Chat/
│   │
│   └── BuildingBlocks/
│
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── Architecture/
│
├── infra/
│
├── docs/
│
├── Vision.md
├── Architecture.md
├── Domain.md
├── AI.md
├── Security.md
└── Development.md

The exact project structure may evolve during implementation.
Do not create projects that have no current responsibility.

6. Backend Architecture
The backend uses:
.NET
ASP.NET Core
Entity Framework Core
REST API
Domain-Driven Design
Modular Monolith
The architecture should distinguish between:
API
Application
Domain
Infrastructure

The exact folder/project arrangement may vary by module.

7. Module Boundaries
Modules represent business capabilities.
Initial modules:
Identity
Organizations
Knowledge
Chat

Modules should own their business logic.
A module should not directly manipulate another module's internal entities or database tables.
Communication between modules should use explicit contracts.
Prefer:
Organizations
    ↓
Application-level contract
    ↓
Knowledge

over:
Knowledge
    ↓
direct access to Organizations EF entities


8. Domain-Driven Design
DDD should be applied pragmatically.
Use DDD concepts when they provide meaningful business boundaries.
Important concepts include:
Entities,
Value Objects,
Aggregates,
Domain Events,
Domain Services,
Application Services / Use Cases.
Do not introduce DDD patterns mechanically.

9. Aggregates
Aggregates should protect business invariants.
An aggregate should have a clear consistency boundary.
The application should modify aggregates through their defined behavior rather than exposing unrestricted setters everywhere.
Do not make every database table an aggregate automatically.

10. Application Layer
The application layer coordinates use cases using CQRS implemented with MediatR.

CQRS Pattern
Use cases are expressed as either commands or queries.

Command — a write operation that changes application state.
Examples: CreateOrganizationCommand, InviteUserCommand, UploadDocumentCommand

Query — a read operation that returns data without changing state.
Examples: GetCurrentOrganizationQuery, GetCurrentUserQuery, GetHealthQuery

Each command and query has a single dedicated handler:
public sealed class CreateOrganizationCommandHandler
    : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResult>

Naming convention:
<UseCase>Command and <UseCase>CommandHandler for write operations.
<UseCase>Query and <UseCase>QueryHandler for read operations.
Result types are named <UseCase>Result.
All three files live together in the same folder inside the module's Application layer.

Handlers should:
validate use-case input,
establish organization context,
invoke domain behavior,
coordinate persistence,
call infrastructure abstractions where required.

Controllers dispatch to handlers through IMediator.Send().
Controllers translate HTTP context into commands or queries and map results to Contracts DTOs.
Business rules must not be placed inside controllers.
Commands, queries, and result types are internal to the module.
Only Contracts DTOs cross the API boundary.

11. API Layer
The API layer uses ASP.NET Core Controllers.
Minimal API is not used.
Controllers should remain thin.
They should primarily:
Receive HTTP input.
Validate request shape.
Resolve authenticated context.
Invoke an application use case.
Return an HTTP response using response DTOs from the Contracts project.
Avoid placing business logic directly in controllers.
Request and response types are defined in the Contracts project.
Do not define inline anonymous response types in controllers.

12. Entity Framework Core
Entity Framework Core is the persistence technology.
Use EF Core for:
persistence,
migrations,
relationships,
indexes,
database configuration.
Avoid introducing a generic repository abstraction over EF Core unless a real requirement appears.
EF Core's DbContext is already an appropriate unit-of-work abstraction for most application scenarios.

13. DbContext and Modules
Modules may own their own DbContext when this improves module boundaries.
For example:
Organizations
    └── OrganizationsDbContext

Knowledge
    └── KnowledgeDbContext

Chat
    └── ChatDbContext

A module's DbContext should primarily contain that module's persistence model.
Modules must not casually query another module's tables through their own DbContext.
The physical database may still be a single Azure SQL database.
Logical module boundaries do not require separate databases.

14. Database Migrations
Database migrations are part of the source-controlled application.
Migrations must be:
deterministic,
reviewable,
committed to source control,
associated with the module that owns the relevant model.
Do not manually modify production databases as part of normal development.

15. Multi-Tenancy Development Rule
Every tenant-owned operation must have an explicit organization context.
Do not accept arbitrary OrganizationId values from the frontend as proof of authorization.
Bad:
POST /organizations/{organizationId}/documents

with the backend assuming that the supplied ID is authorized.
The backend must validate membership and authorization.
The effective organization context should come from authenticated application state and validated membership.

16. Authentication Development Rule
Authentication is delegated to Microsoft Entra / Entra External ID.
Do not implement:
password hashing,
login sessions,
custom JWT generation,
custom identity protocols,
unless explicitly required by a future architectural decision.
The backend should validate identity-provider-issued tokens.

17. Authorization Development Rule
Authorization uses ASP.NET Core policy-based authorization.
Prefer:
[Authorize(Policy = Policies.Documents.Read)]

over direct role checks inside controllers.
Business roles belong to application membership.
Authorization policies map roles/capabilities to allowed operations.

18. Frontend Architecture
The frontend uses:
React
TypeScript
Vite
MSAL
Prefer feature-oriented organization.
Example:
src/
├── app/
├── features/
│   ├── auth/
│   ├── organizations/
│   ├── knowledge/
│   └── chat/
├── components/
├── services/
└── types/

Avoid creating a large global component hierarchy before the application requires it.

19. API Client
The frontend should communicate with the backend through a consistent API client.
Authentication tokens should be handled centrally.
Individual components should not implement authentication logic independently.
Prefer:
Component
   ↓
Feature API
   ↓
Shared API Client
   ↓
ASP.NET Core API


20. Validation
Validate input at appropriate boundaries.
Frontend validation improves user experience.
Backend validation is authoritative.
Domain invariants must be enforced by the backend/domain model.
Never rely on frontend validation for security.

21. Testing Strategy
Testing should focus on business behavior.
Unit Tests
Use unit tests for:
domain rules,
value objects,
authorization decisions,
important application logic.
Integration Tests
Use integration tests for:
API behavior,
authentication/authorization integration where practical,
EF Core persistence,
organization isolation,
database behavior.
Architecture Tests
Architecture tests may be introduced to enforce:
module boundaries,
dependency direction,
forbidden references.
Do not write tests purely to increase coverage percentages.

22. Tenant Isolation Tests
Tenant isolation is security-critical.
At minimum, integration tests should verify scenarios such as:
Organization A
    ↓
Document A

Organization B
    ↓
Document B

A user belonging to Organization A must not be able to:
read Document B,
modify Document B,
delete Document B,
retrieve Document B through search,
access Organization B's conversations.
These tests are mandatory for tenant-owned features.

23. Azure Development
Azure resources should be introduced incrementally.
Do not create the entire production infrastructure before the application needs it.
The first stages may use local development equivalents where practical.
Example:
Local development
    ↓
Local database / development Azure SQL
    ↓
Development Azure resources
    ↓
Production Azure resources

The exact local-development strategy should be decided per dependency.

24. Infrastructure as Code
All Azure infrastructure should eventually be represented through Infrastructure as Code.
Infrastructure should be reproducible.
Avoid manually configuring production resources unless necessary.
Infrastructure code belongs under:
infra/

Infrastructure should not leak Azure-specific implementation details into the domain layer.

25. Azure Resource Dependencies
Introduce infrastructure in dependency order.
A likely progression is:
Resource Group
    ↓
Azure SQL
    ↓
Storage Account
    ↓
Key Vault
    ↓
App Service
    ↓
Managed Identity permissions
    ↓
Application Insights
    ↓
AI Search
    ↓
Azure OpenAI

The exact order may change based on implementation requirements.

26. AI Development
AI functionality should be implemented only after the basic authenticated application works.
The first AI milestone is:
Upload PDF
    ↓
Store document
    ↓
Process document
    ↓
Index knowledge
    ↓
Ask question
    ↓
Retrieve organization-scoped knowledge
    ↓
Generate grounded answer

AI services must never be used as an authorization mechanism.

27. AI Retrieval Security
Every AI Search query must be scoped to the current Organization.
The retrieval layer must never perform unrestricted searches across all customer data.
The application determines which chunks are allowed to reach Azure OpenAI.
Prompt instructions are not considered a security boundary.

28. Coding Conventions
Prefer:
clear names,
explicit dependencies,
small classes,
small methods,
immutable data where appropriate,
dependency injection,
asynchronous APIs,
cancellation support,
meaningful exceptions,
structured logging.
Avoid:
magic strings,
hidden global state,
unnecessary static helpers,
massive service classes,
deeply nested conditional logic,
premature abstractions.

29. Naming
Use clear and consistent naming.
Examples:
Organization
ApplicationUser
Membership
Document
Conversation
ChatMessage

Use business terminology from Domain.md.
Do not introduce alternative names for established domain concepts without a strong reason.
For example, do not use:
Tenant
Company
CustomerAccount
Workspace

interchangeably with Organization.
The canonical domain term is:
Organization


30. Error Handling
Errors should be predictable and consistent.
The API should use appropriate HTTP status codes.
Security-sensitive errors must not reveal information about resources belonging to other organizations.
Prefer behavior equivalent to:
404 Not Found

when revealing the existence of an inaccessible resource would create an information leak.

31. Logging
Use structured logging.
Logs should help diagnose:
authentication failures,
authorization failures,
application errors,
document processing failures,
AI failures,
infrastructure failures.
Never log:
passwords,
access tokens,
refresh tokens,
API keys,
storage credentials,
unnecessary document contents,
unnecessary chat contents.

32. AI Agent Rules
AI coding agents working in this repository must follow these rules.
Read Before Changing
Before implementing a feature, inspect the relevant:
Vision.md
Architecture.md
Domain.md
AI.md
Security.md
Development.md
Do not assume the repository architecture from generic framework conventions.
Preserve Existing Decisions
Do not introduce architectural changes merely because another pattern is more familiar.
If an implementation conflicts with an architectural document, stop and explain the conflict.
Small Changes
Prefer small, reviewable changes.
Do not modify unrelated parts of the repository while implementing a feature.
No Speculative Infrastructure
Do not create Azure resources, abstractions, modules, or services that are not required by the current milestone.
No Silent Architecture Changes
If a feature requires changing:
module boundaries,
persistence strategy,
identity architecture,
multi-tenancy model,
authorization model,
AI architecture,
explain the proposed change before implementing it.

33. AI Agent Workflow
For each feature, the preferred workflow is:
1. Understand the requirement
        ↓
2. Inspect relevant architecture/domain rules
        ↓
3. Identify affected module(s)
        ↓
4. Propose implementation approach
        ↓
5. Implement the smallest useful change
        ↓
6. Add/update tests
        ↓
7. Run build
        ↓
8. Run relevant tests
        ↓
9. Fix failures
        ↓
10. Summarize changes

The agent should not immediately start editing files without understanding the relevant architecture.

34. Definition of Done
A feature is considered complete when:
the implementation is consistent with the architecture,
the domain rules are respected,
authorization is enforced,
organization isolation is preserved,
relevant tests exist,
the solution builds successfully,
relevant tests pass,
no unnecessary infrastructure was introduced,
no secrets were introduced,
the change is understandable to another developer.

35. Development Milestones
The project should progress through the following milestones.
Milestone 0 — Repository Skeleton
Goal:
Repository
    ↓
.NET solution
    ↓
React application
    ↓
Build succeeds

No business functionality is required.

Milestone 1 — First Working Application
Goal:
React
    ↓
ASP.NET Core
    ↓
Health endpoint

Both applications run locally.

Milestone 2 — Database
Goal:
ASP.NET Core
    ↓
EF Core
    ↓
Azure SQL / development database

The application can persist and retrieve basic data.

Milestone 3 — Authentication
Goal:
React
    ↓
MSAL
    ↓
Entra / External ID
    ↓
ASP.NET Core

The backend recognizes the authenticated user.

Milestone 4 — Organization Onboarding
Goal:
Authenticated User
    ↓
Create Organization
    ↓
Membership
    ↓
OrganizationOwner

The user can reach the organization dashboard.

Milestone 5 — Organization Security
Goal:
User A
    ↓
Organization A

User B
    ↓
Organization B

Users cannot access each other's organization data.

Milestone 6 — Invitations
Goal:
OrganizationOwner
    ↓
Invite User
    ↓
User accepts
    ↓
Membership created


Milestone 7 — Documents
Goal:
User
    ↓
Upload PDF
    ↓
Blob Storage
    ↓
Document record


Milestone 8 — Knowledge Processing
Goal:
PDF
    ↓
Text extraction
    ↓
Chunking
    ↓
Embeddings
    ↓
Azure AI Search


Milestone 9 — RAG Chat
Goal:
Question
    ↓
Organization-scoped retrieval
    ↓
Azure OpenAI
    ↓
Grounded answer
    ↓
Sources


36. The Golden Rule
At every stage ask:
"Can I run the application and see something working?"
If the answer is yes, continue.
If the answer is no, prefer completing the smallest end-to-end slice before adding another architectural layer.
The goal is not to build the perfect architecture first.
The goal is to build a working product while preserving the architectural boundaries that matter.


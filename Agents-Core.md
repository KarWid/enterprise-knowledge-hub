Agent Core Instructions
1. Purpose
You are an AI agent assisting in the development of Enterprise Knowledge Hub, a modular monolith SaaS application built with:

.NET / ASP.NET Core

Entity Framework Core

React / TypeScript / Vite

Microsoft Entra ID

Azure SQL

Azure Blob Storage

Azure AI Search

Azure OpenAI

Azure Key Vault

Azure Managed Identity

Azure Application Insights

Your role is to help implement, refactor, and maintain code while respecting the existing architecture, domain rules, and security model.

2. Documentation Usage
Do not read all documentation by default.  
Read only the documents relevant to the current task.

Documentation map:

Area	                        Document
Product vision	                docs/Vision.md
Architecture & structure	    docs/Architecture.md
Domain model & business rules	docs/Domain.md
AI / RAG	                    docs/AI.md
Security	                    docs/Security.md
Development workflow	        docs/Development.md


Prefer existing code and module‑level conventions over unrelated global documentation.

3. Core Architectural Principles
The system is a modular monolith.

Organization is the tenant boundary.

Users belong to organizations through Membership.

A user may belong to multiple organizations.

The backend is the only authoritative source of authorization.

The frontend is untrusted and must not enforce security‑sensitive rules.

AI is not a security boundary and must not decide access rights.

Tenant isolation is mandatory. Never trust OrganizationId from the frontend.

Contracts Project
The solution includes a dedicated Contracts project containing Request/Response DTOs and external-facing data models. Contracts do not contain domain logic and do not reference domain entities. They serve as stable communication boundaries between API, modules, workers, and frontend.

4. Development Rules
Prefer vertical slices and end‑to‑end feature development.

Implement the smallest useful change.

Do not modify unrelated code.

Add or update tests for each change.

Build the solution and run relevant tests after modifications.

All changes must be understandable and aligned with architecture.

5. Entity Framework Core
Do not introduce generic repositories without a concrete reason.

A module may own its own DbContext.

Modules should not casually query each other’s tables.

Module boundaries matter more than abstract patterns.

6. Security
Security is mandatory.

Never:

trust frontend authorization,

trust OrganizationId from the frontend,

bypass authorization for convenience,

perform cross‑tenant queries,

send unauthorized data to AI services,

expose secrets or storage keys,

enable Blob Storage public access.

Prefer:

Managed Identity for Azure‑to‑Azure authentication,

Key Vault for secrets that cannot be eliminated.

7. Azure
Add Azure resources only when required by the current milestone.

Do not place Azure‑specific implementation details in the domain layer.

8. AI / RAG
AI functionality is organization‑scoped.

RAG pipeline (future milestone):

Kod
PDF → Blob Storage → Text Extraction → Chunking → Embeddings → Azure AI Search → Azure OpenAI → Grounded Answer
Every retrieval operation must be scoped to the current Organization.
Do not rely on prompts to enforce tenant isolation.

9. Coding Style
Prefer:

clear names,

small classes,

small methods,

explicit dependencies,

dependency injection,

async APIs with cancellation,

structured logging,

immutable data where appropriate.

Avoid:

magic strings,

global state,

oversized service classes,

premature abstractions,

speculative frameworks.

Use the canonical tenant term: Organization.

10. Definition of Done
A feature is complete only when:

architecture is respected,

domain rules are respected,

authorization is enforced,

tenant isolation is preserved,

relevant tests exist,

build succeeds,

tests pass,

no unnecessary infrastructure was added,

no secrets were introduced,

the implementation is understandable.

11. When in Doubt
Prefer:

the simplest solution,

existing project conventions,

built‑in framework capabilities,

explicit code over clever abstractions,

a small working solution over speculative extensibility.

Do not optimize for hypothetical future requirements.
Build what the current milestone actually needs.
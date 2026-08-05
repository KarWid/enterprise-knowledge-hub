Enterprise Knowledge Hub — Domain Model
1. Domain Overview
The domain is centered around organizational knowledge.
The primary business concepts are:
Organization
    │
    ├── Membership
    │       │
    │       └── ApplicationUser
    │
    ├── Documents
    │
    ├── Knowledge
    │
    └── Conversations

The application is multi-tenant.
Organization is the primary business boundary for customer data.

2. Organization
Organization represents a customer/company using Enterprise Knowledge Hub.
It is an application-level concept.
An Organization does not require a direct one-to-one relationship with a Microsoft Entra tenant.
Example:
Organization
----------------
Id
Name
CreatedAt
Status

Potential future properties may include:
subscription information,
branding,
configuration,
feature settings.
These should only be introduced when required.

3. ApplicationUser
ApplicationUser represents a person known to the application.
The application user is separate from the external identity provider.
Example:
ApplicationUser
----------------
Id
ExternalIdentityId
Email
DisplayName
CreatedAt
Status

ExternalIdentityId identifies the corresponding identity in the configured identity provider.
The domain should not depend directly on Microsoft Entra-specific identity structures.

4. Membership
Membership represents the relationship between a user and an organization.
ApplicationUser
       │
       ▼
Membership
       │
       ▼
Organization

Example:
Membership
----------------
Id
UserId
OrganizationId
Role
Status
CreatedAt

Membership is the location of organization-specific authorization.
This design allows a user to belong to multiple organizations.
Example:
John
 │
 ├── ACME
 │     └── OrganizationAdmin
 │
 └── Contoso
       └── Employee


5. Organization Roles
Initial business roles may include:
OrganizationOwner
The primary organization owner.
Can:
manage organization settings,
invite and remove users,
manage organization-level configuration,
access all organization capabilities.
OrganizationAdmin
Can perform delegated administrative operations.
KnowledgeManager
Can manage organizational knowledge sources.
Employee
Can consume organizational knowledge and use the AI chat.
Roles are business concepts.
Authorization policies determine the actual permissions granted by those roles.

6. Organization Onboarding
The first authenticated user can create an Organization.
The onboarding flow is:
Authenticated User
        ↓
Create Organization
        ↓
Create Membership
        ↓
Role = OrganizationOwner

The organization is created in the application database.
The application does not require an administrator to manually create an Azure or Microsoft Entra tenant for the organization.

7. User Invitation
Organization Owners and authorized administrators can invite users.
Conceptually:
Organization Owner
        ↓
Invitation
        ↓
User accepts
        ↓
External authentication
        ↓
ApplicationUser
        ↓
Membership

The identity provider remains responsible for authenticating the user.
The application remains responsible for creating the organization membership.

8. Document
Document represents a source document uploaded to the knowledge system.
For the MVP, supported documents are PDF files.
Example:
Document
----------------
Id
OrganizationId
Name
BlobReference
ContentType
Status
CreatedBy
CreatedAt

Possible document states:
Uploaded
Processing
Ready
Failed
Archived


9. Document Ownership
Every Document belongs to exactly one Organization.
Organization
     │
     └── Document

A document must never be accessible outside its owning organization.
The application must validate organization ownership for every document operation.

10. Knowledge
Knowledge represents information derived from organization-owned sources.
For the MVP, knowledge is derived from PDF documents.
Conceptually:
Document
   ↓
Processing
   ↓
Chunks
   ↓
Embeddings
   ↓
Search Index

Knowledge is not the same as the original document.
The original document remains in Blob Storage.
Searchable representations are maintained in Azure AI Search.

11. Knowledge Source
The architecture should allow different knowledge sources in the future.
Potential types include:
Document
Database
Transcript
Audio
SharePoint
External System

The MVP implements only document-based knowledge.
Future knowledge source types should not be implemented until there is a concrete requirement.

12. Conversation
A Conversation represents a user's interaction with the knowledge assistant.
A conversation belongs to an Organization and is associated with the user who initiated it.
Conceptually:
Organization
     │
     └── Conversation
              │
              └── Messages

The conversation must execute within the user's organization context.

13. Chat Message
A chat message represents one user question or assistant response.
Example:
ChatMessage
----------------
Id
ConversationId
Role
Content
CreatedAt

Potential roles:
User
Assistant
System

The MVP should keep the model simple.

14. Retrieval
When a user asks a question:
User
 ↓
Conversation
 ↓
Organization Context
 ↓
Authorization
 ↓
Knowledge Retrieval
 ↓
Relevant Chunks
 ↓
AI Generation

Retrieval must be scoped to the current Organization.
The AI model must never be used as an authorization mechanism.

15. Source References
AI responses should be able to reference the knowledge sources used to construct the answer.
For example:
Answer

Sources:
- Pricing_2026.pdf
- Meeting_2026_07_15.pdf

The exact source-reference model can evolve with the retrieval implementation.

16. Domain Boundaries
The initial domain may be divided into the following conceptual areas:
Identity / Access
    Organization
    ApplicationUser
    Membership
    Invitation

Knowledge
    Document
    Knowledge Source

Conversation
    Conversation
    Chat Message

The exact module boundaries are implementation details and may evolve as the application grows.

17. Domain Rules
Important domain rules include:
An Organization must have a unique application identifier.
A Membership must reference an existing Organization and ApplicationUser.
A user's role is defined per Membership.
The first user creating an Organization becomes its OrganizationOwner.
Organization-owned data must never cross organization boundaries.
A Document belongs to exactly one Organization.
Conversations execute within an Organization context.
Authorization is based on application-owned membership and permissions.
External identity providers authenticate users but do not own application authorization rules.
AI services must never determine whether a user is authorized to access data.

18. Explicit Non-Goals for the MVP
The following are intentionally outside the initial domain implementation:
database knowledge sources,
audio knowledge sources,
speech-to-text,
SharePoint integration,
cross-organization knowledge,
complex subscription management,
organization hierarchy,
advanced delegation models,
separate databases per organization,
microservices.
These may be introduced later if the product requires them.



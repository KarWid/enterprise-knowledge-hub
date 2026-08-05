Enterprise Knowledge Hub — Product Vision
1. Product Vision
Enterprise Knowledge Hub is a multi-tenant SaaS application that enables organizations to capture, search, and interact with their internal knowledge using AI.
The primary goal is to make organizational knowledge accessible at the moment it is needed.
Instead of relying on tribal knowledge, employees should be able to ask questions such as:
"What was discussed during the last meeting?"
"What is the current price of this product?"
"What does our regulation say about this process?"
"What was agreed with the customer?"
The system should provide answers grounded in the organization's own knowledge sources and clearly indicate the supporting sources.
The initial version focuses exclusively on PDF documents.
Future versions may extend the knowledge layer to include additional sources such as databases, meeting transcripts, audio recordings, and other enterprise systems.

2. Product Model
Enterprise Knowledge Hub is a multi-tenant SaaS product.
An Organization is a first-class concept owned by the application domain.
Organizations are isolated from each other at the application and data levels.
A user belongs to one or more organizations through an explicit Membership.
The application does not assume that an organization must have its own Microsoft Entra tenant.
Authentication and organization membership are separate concepts:
Identity providers answer "Who is this user?"
The application answers "Which organization does this user belong to?"
Application authorization answers "What can this user do?"
Microsoft Entra / Entra External ID is used as the primary identity platform, while organization membership and business roles are owned by the application.

3. Initial Target Users
The primary users are:
Organization Owner
The person responsible for setting up the organization and managing its users.
Responsibilities include:
creating the organization,
inviting users,
managing organization-level settings,
managing access to knowledge,
managing organization-level features.
Organization Administrator
A user with delegated administrative responsibilities.
Employee
A regular organization member who primarily consumes organizational knowledge through the AI chat.

4. Initial User Journey
The initial onboarding experience should be simple and self-service.
A new customer:
Opens the application.
Creates or signs into an account using a supported identity provider.
Creates an Organization if one does not yet exist.
Becomes the Organization Owner.
Invites additional users.
Users join the organization through invitations.
The application should not require the product operator to manually provision an Azure environment or Microsoft Entra tenant for each customer.

5. Knowledge Hub — MVP
The MVP supports one knowledge source:
PDF documents.
The initial flow is:
User
  ↓
Upload PDF
  ↓
Blob Storage
  ↓
Document Processing
  ↓
Text Extraction / Chunking
  ↓
Embeddings
  ↓
Azure AI Search
  ↓
User Question
  ↓
Retrieval
  ↓
Azure OpenAI
  ↓
Grounded Answer + Sources

The AI should answer using information retrieved from the organization's own knowledge sources.
The system should avoid presenting unsupported information as fact.

6. Future Knowledge Sources
The architecture should allow future knowledge sources without requiring a redesign of the entire application.
Potential future sources include:
database records,
meeting transcripts,
audio recordings,
SharePoint,
enterprise applications,
other document formats.
These are explicitly out of scope for the MVP.

7. Privacy and Security Vision
Enterprise Knowledge Hub is designed as an enterprise-oriented SaaS product.
Customer data must remain isolated between organizations.
The application should follow these principles:
least-privilege access,
organization-level data isolation,
managed identities where supported,
secrets stored in Azure Key Vault,
public access to Blob Storage disabled,
AI access restricted to application-controlled data,
no unnecessary exposure of customer data,
auditable access to sensitive operations.
Azure OpenAI and Azure AI Search are application infrastructure components and should not be treated as independent sources of customer authorization.
Authorization must be enforced by the application before customer data is retrieved or processed.

8. Technology Direction
The initial technology stack is:
Frontend
React
Vite
TypeScript
MSAL
Backend
.NET
ASP.NET Core
REST API
Entity Framework Core
Domain-Driven Design
Modular Monolith
Data
Azure SQL Database
Azure Blob Storage
AI
Azure AI Search
Azure OpenAI
Identity
Microsoft Entra / Microsoft Entra External ID
Application-owned organization membership and authorization
Azure Infrastructure
Azure App Service
Azure Key Vault
Managed Identity
Application Insights
Azure Monitor
Infrastructure should be defined using Infrastructure as Code.

9. Architectural Direction
The application should start as a modular monolith.
The architecture should support clear domain boundaries without introducing distributed-system complexity prematurely.
The system should optimize for:
simplicity,
maintainability,
security,
testability,
clear domain boundaries,
future extensibility.
The architecture should not optimize prematurely for scale or microservices.

10. Product Principles
Build the smallest useful system
The first goal is a working end-to-end knowledge experience.
Keep domain concepts explicit
Organization, User, Membership, Document, Knowledge Source, and Conversation should have clear ownership and responsibilities.
Separate identity from authorization
Authentication establishes identity.
The application owns organization membership and business authorization.
Secure by default
Sensitive Azure resources should not be publicly accessible.
AI must be grounded
The AI should primarily answer from retrieved organizational knowledge and provide supporting sources.
Design for extension, not speculation
Future knowledge sources should be possible without building them before they are needed.
Prefer a modular monolith
Keep deployment and operational complexity low until there is a concrete reason to distribute components.

11. MVP Success Criteria
The MVP is successful when an organization can:
Create an account.
Create an organization.
Invite users.
Authenticate securely.
Upload PDF documents.
Process documents into searchable knowledge.
Ask questions through the chat.
Receive answers grounded in uploaded documents.
See supporting sources.
Be isolated from all other organizations' data.



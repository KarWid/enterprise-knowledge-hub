AI Architecture
This document describes the artificial intelligence architecture of Enterprise Knowledge Hub, including Retrieval-Augmented Generation (RAG), knowledge ingestion, retrieval, generation, security considerations, and future AI capabilities.

1. AI Vision
Artificial Intelligence is an enabling capability of Enterprise Knowledge Hub.
The purpose of AI is not to replace organizational knowledge or become the source of truth.
The purpose of AI is to make existing organizational knowledge accessible through natural language interaction.
The system follows the principle:
The organization owns the knowledge. AI helps users discover and understand it.

2. AI Architecture Overview
Enterprise Knowledge Hub uses Retrieval-Augmented Generation (RAG).
RAG combines:
information retrieval,
semantic search,
large language models.
Instead of asking an AI model to answer from its own training data, the system first retrieves relevant organization-owned information and then provides this context to the language model.
High-level flow:
User Question

        |

        v

Chat Module

        |

        v

AI Orchestrator

        |

        +----------------+

        | Knowledge      |

        | Retrieval     |

        +----------------+

                |

                v

        Azure SQL Vector Retrieval

                |

                v

     Relevant Knowledge Fragments

                |

                v

        Azure OpenAI

                |

                v

          Generated Answer

                |

                v

             User


3. Core AI Components
3.1 Azure OpenAI
Azure OpenAI provides the large language model capabilities.
Responsibilities:
answer generation,
natural language understanding,
summarization,
conversational context handling.
Azure OpenAI does not store organizational knowledge.
The model receives only:
user question,
retrieved knowledge context,
system instructions.

3.2 Azure SQL Knowledge Retrieval
The planned MVP uses Azure SQL for chunk/vector storage and similarity queries,
replacing the earlier Azure AI Search plan to avoid a separate search service.
Vector storage/retrieval remain future slices.
Responsibilities:
persisting knowledge fragments and their vectors,
vector similarity search,
filtering by validated Organization and document access.
The service allows the system to find relevant information before generating an answer.

3.3 Azure Storage
Azure Storage is used for storing original documents.
Responsibilities:
storing uploaded files,
maintaining original document versions,
providing secure document access.
Example:
Document.pdf

        |

        v

Azure Blob Storage

        |

        v

Processing Pipeline

        |

        v

Knowledge Index


4. Retrieval-Augmented Generation Flow
Question Flow
Example question:
"What is the current price of product X?"
Processing:
User Question

↓

Question Understanding

↓

Generate Search Query

↓

Azure SQL Vector Retrieval

↓

Retrieve Relevant Chunks

↓

Build AI Context

↓

Azure OpenAI

↓

Generate Answer

↓

Return Response + Sources


5. Knowledge Ingestion Pipeline
Documents become available for AI through an ingestion process.
MVP pipeline (upload/basic verification and Markdown extraction are implemented;
PDF extraction, chunking, embeddings, and retrieval remain planned):
PDF or Markdown Upload

    |

    v

Azure Blob Storage

    |

    v

Document Processing Worker

    |

    v

Text Extraction

    |

    v

Content Chunking

    |

    v

Embedding Generation

    |

    v

Azure SQL Chunks and Vectors

    |

    v

Available Knowledge


6. Document Processing
Text Extraction
The system extracts readable content from uploaded documents.
Markdown extraction persists bounded normalized UTF-8 text in Azure SQL. It
preserves Markdown as data without rendering HTML or fetching resources. Durable
work/results retain Organization, Document, and validated source ETag. Format flags
control extraction independently of uploads. See [Text extraction](DocumentTextExtraction.md).
Future extensions may include:
OCR,
scanned documents,
tables,
images.

Chunking
Large documents are divided into smaller knowledge fragments.
Example:
Document

    |

    +-- Chunk 1

    +-- Chunk 2

    +-- Chunk 3

Chunking allows:
better search accuracy,
smaller AI context,
improved response quality.
Chunking strategy should be configurable because different document types may require different approaches.

Embeddings
Each knowledge fragment is transformed into a numerical representation.
Example:
Text Fragment

        |

        v

Embedding Model

        |

        v

Vector Representation

Embeddings allow semantic similarity search.
Example:
Question:
"How much does the premium package cost?"
Document:
"The price of the enterprise plan is..."
The system can identify the relationship even when wording differs.

7. Knowledge Retrieval Abstraction
The AI layer must not depend directly on documents.
The system uses a knowledge retrieval abstraction:
public interface IKnowledgeRetriever
{
    Task<IEnumerable<KnowledgeResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}

Possible implementations:
DocumentKnowledgeRetriever

DatabaseKnowledgeRetriever

TranscriptKnowledgeRetriever

The MVP implements only document retrieval.

8. Prompt Architecture
Prompts are treated as application configuration, not hardcoded strings.
The system should separate:
System Instructions
Define AI behavior.
Example:
answer only from provided context,
do not invent information,
indicate when information is unavailable.

Context
Retrieved knowledge provided during runtime.

User Input
The actual user question.
Final structure:
System Prompt

+

Retrieved Knowledge

+

User Question

=

AI Response

Retrieved knowledge must be supplied as clearly labeled source material with
source identifiers, separate from application instructions. Never place retrieved
text in the system instructions. Application instructions must tell the model to
treat source content as data and ignore instructions embedded in it.

The backend must enforce Organization and document access before retrieval.
Retrieved content cannot initiate tool actions or grant permissions; the MVP chat
provides no action tools. Format and encoding checks do not detect prompt injection.
Follow the authoritative [prompt injection and untrusted document content rules](Security.md#prompt-injection-and-untrusted-document-content).


9. AI Response Requirements
AI responses should prioritize:
Accuracy
The answer should be based on retrieved organizational information.

Transparency
The system should provide references to source documents.
Example:
Answer:

"The product price is 120 EUR."

Sources:

- Pricing_2026.pdf
- Page 3


Uncertainty Handling
The AI should not fabricate information.
If no reliable information is found:
"I could not find this information in available company knowledge sources."


10. Security and Privacy
Security is a primary AI design requirement.
The system follows these principles:
customer data remains within the customer's Azure environment,
AI requests are authenticated,
access permissions are validated before retrieval,
tenant data is isolated,
secrets are stored in Azure Key Vault,
services communicate using Managed Identity where possible.

11. Data Isolation in AI
Multi-tenancy applies also to AI retrieval.
Search operations must always include organization context.
Example:
Search Request

{
    OrganizationId: "company-a",
    Query: "pricing information"
}

The system must never retrieve knowledge belonging to another organization.

12. AI Limitations in MVP
The first version intentionally does not support:
autonomous agents,
external actions,
database reasoning,
workflow execution,
voice processing,
automatic decision making.
The AI assistant provides information retrieval and explanation only.

13. Future AI Capabilities
The architecture allows future expansion.
Additional Knowledge Sources
Examples:
Database

↓

Knowledge Retriever

↓

AI Assistant

Meeting Recording

↓

Speech To Text

↓

Knowledge

↓

AI Assistant


AI Agents
Future versions may introduce agents capable of:
preparing reports,
summarizing information,
assisting workflows,
interacting with enterprise systems.
Agents should always operate within defined permissions.

14. AI Engineering Principles
The AI implementation follows these principles:
AI is not the source of truth.
Retrieved organizational knowledge is the source of truth.
Every answer should be explainable.
Hallucination should be minimized through retrieval constraints.
Knowledge sources must remain replaceable.
AI components should be isolated from business domains.
Security and tenant isolation apply to every AI operation.

15. Summary
Enterprise Knowledge Hub uses AI as a knowledge access layer.
The MVP implementation uses Retrieval-Augmented Generation based on:
Azure Blob Storage,
Azure SQL chunks, vectors, and Organization-scoped retrieval,
Azure OpenAI.
The architecture intentionally separates:
knowledge ownership,
document management,
retrieval,
AI generation.
This approach allows the platform to evolve from document-based search into a complete enterprise knowledge intelligence platform.


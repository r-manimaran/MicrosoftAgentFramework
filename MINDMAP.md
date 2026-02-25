# Microsoft Agent Framework - Mindmap

```
MICROSOFT AGENT FRAMEWORK
│
├─── 1. FUNDAMENTALS
│    ├─── Agent Creation
│    │    ├─── Basic Agent (01-ZeroToFirstAgent)
│    │    ├─── Agent Configuration (02-AIAgentSettings)
│    │    │    ├─── Instructions & System Prompts
│    │    │    ├─── Name & Description
│    │    │    ├─── Client Factory
│    │    │    ├─── Logger Factory
│    │    │    ├─── Service Provider (DI)
│    │    │    └─── ChatClientAgentOptions
│    │    └─── YAML-based Agents (24-AIAgent_YAML)
│    │
│    ├─── Response Types
│    │    ├─── Synchronous Responses
│    │    ├─── Streaming Responses
│    │    └─── Structured Output (06-StructuredOutput)
│    │         ├─── Generic RunAsync<T>()
│    │         ├─── JSON Schema
│    │         └─── Custom Serialization
│    │
│    └─── State Management
│         ├─── Persistent Conversations (03-PersistentConversation)
│         │    ├─── AgentThread Serialization
│         │    ├─── JSON Storage
│         │    └─── Thread Restoration
│         ├─── User Memory (21-UserMemoryToAgent)
│         └─── Persistent Storage (20-PersistentStorage)
│
├─── 2. LLM PROVIDERS
│    ├─── Azure OpenAI
│    ├─── OpenAI
│    ├─── Anthropic Claude
│    ├─── Google Gemini
│    ├─── Ollama (Local)
│    ├─── XAi Grok
│    ├─── Foundry Local
│    └─── Multiple LLMs (18-MultipleLLMAgents)
│
├─── 3. TOOL CALLING & FUNCTIONS
│    ├─── Basic Tools (04-ToolCalling/Basic)
│    │    ├─── Time Tools
│    │    ├─── AIFunctionFactory
│    │    └─── Description Attributes
│    │
│    ├─── Advanced Tools (04-ToolCalling/Advanced)
│    │    ├─── File System Operations
│    │    ├─── Security Guards
│    │    ├─── User Approval Workflow
│    │    ├─── Middleware
│    │    └─── Reflection-based Registration
│    │
│    ├─── MCP Integration (04-ToolCalling/MCP)
│    │    ├─── Model Context Protocol
│    │    └─── Remote MCP (19-AgentAsRemoteMCP)
│    │
│    ├─── Service Injection (04-ToolCalling/ServiceInjection)
│    └─── Toolkit Simplification (25-AgentFrameworkToolKit)
│
├─── 4. MULTI-AGENT SYSTEMS
│    ├─── Agent Patterns
│    │    ├─── Single Agent
│    │    ├─── Multi-Agent
│    │    ├─── Delegation Pattern (07-AgentCallingAgent)
│    │    ├─── Jack-of-All-Trades Pattern
│    │    └─── Agent-as-Tool (AsAIFunction)
│    │
│    ├─── Workflow Orchestration (10-MultiAgents-Workflows)
│    │    ├─── Sequential Workflow
│    │    │    ├─── Linear Execution
│    │    │    └─── IntentAgent → EmailAgent → ToneAgent
│    │    │
│    │    ├─── Concurrent Workflow
│    │    │    ├─── Parallel Execution
│    │    │    └─── Multiple Agents Simultaneously
│    │    │
│    │    ├─── Handoff Workflow
│    │    │    ├─── Dynamic Control Transfer
│    │    │    └─── Agent ↔ Agent Communication
│    │    │
│    │    └─── Human-in-the-Loop (26-WorkflowHumanInLoop)
│    │         ├─── User Approval
│    │         ├─── Pause/Resume
│    │         └─── Interactive Workflows
│    │
│    ├─── Workflow Components
│    │    ├─── AgentWorkflowBuilder
│    │    ├─── StreamingRun
│    │    ├─── WorkflowEvent
│    │    ├─── TurnToken
│    │    └─── InProcessExecution
│    │
│    └─── Specialized Multi-Agent
│         └─── Multi-Agent with Structured Output (09-MultiAgent-StructuredOutput)
│
├─── 5. RAG (Retrieval-Augmented Generation)
│    ├─── Basic RAG (11-AIAgent-RAG-Basic)
│    │    ├─── Scenario 1: Context Stuffing
│    │    │    └─── Pre-load all data (high tokens)
│    │    │
│    │    ├─── Scenario 2: Vector Search
│    │    │    ├─── Embeddings
│    │    │    ├─── Similarity Search
│    │    │    └─── Optimized Tokens
│    │    │
│    │    └─── Scenario 3: Tool-based RAG
│    │         └─── Agent uses search as tool
│    │
│    ├─── Advanced RAG (12-AIAgent-RAG-Advanced)
│    │    ├─── Option 1: Rephrase Question
│    │    ├─── Option 2: Enhanced Embedding
│    │    ├─── Option 3: Common Sense
│    │    └─── SQL Server Vector Store
│    │
│    ├─── Vector Stores
│    │    ├─── InMemory Vector Store
│    │    ├─── Azure AI Search
│    │    ├─── SQL Server 2025
│    │    └─── Cosmos NoSQL
│    │
│    ├─── Embedding Generation
│    │    ├─── text-embedding-3-small
│    │    ├─── IEmbeddingGenerator
│    │    └─── Enhanced Embeddings
│    │
│    └─── Search Tools (15-AI-Foundry)
│         ├─── File Search
│         └─── Web Search
│
├─── 6. MULTIMODAL CAPABILITIES
│    ├─── Image Processing (08-ImageAndPdf)
│    │    ├─── Remote URL Images
│    │    ├─── Local File Images
│    │    ├─── Base64 Encoding
│    │    └─── Memory-based Images
│    │
│    └─── PDF Processing (08-ImageAndPdf)
│         ├─── PDF Summarization
│         └─── Document Analysis
│
├─── 7. REASONING & OPTIMIZATION
│    ├─── Reasoning Control (14-Reasoning)
│    │    ├─── Reasoning Effort Levels
│    │    │    ├─── Minimal
│    │    │    ├─── Low
│    │    │    ├─── Medium (default)
│    │    │    └─── High
│    │    │
│    │    ├─── Reasoning Models
│    │    │    ├─── OpenAI (GPT-4o, o1-preview, o1-mini)
│    │    │    ├─── Azure OpenAI
│    │    │    ├─── Anthropic Claude
│    │    │    └─── Google Gemini
│    │    │
│    │    └─── Reasoning Token Tracking
│    │
│    ├─── Token Management (05-TokenUsage)
│    │    ├─── Input Token Tracking
│    │    ├─── Output Token Tracking
│    │    ├─── Reasoning Token Tracking
│    │    ├─── Streaming Token Usage
│    │    └─── Cost Analysis
│    │
│    └─── Serialization Optimization (17-TOON-For-LLM)
│         ├─── JSON Serialization
│         ├─── TOON Serialization
│         ├─── Token Efficiency Comparison
│         └─── Cost Optimization
│
├─── 8. OBSERVABILITY & MONITORING
│    ├─── OpenTelemetry (13-AIAgentOpenTelemetry)
│    │    ├─── Tracing
│    │    ├─── Console Telemetry
│    │    ├─── Azure Application Insights
│    │    ├─── Sensitive Data Logging
│    │    └─── Distributed Tracing
│    │
│    ├─── Telemetry Data
│    │    ├─── Token Usage Metrics
│    │    ├─── Agent Information
│    │    ├─── Conversation Data
│    │    ├─── Performance Metrics
│    │    └─── Success/Failure Status
│    │
│    ├─── Middleware
│    │    ├─── Function Call Logging
│    │    ├─── Approval Handling
│    │    └─── Custom Middleware
│    │
│    └─── Kusto Queries
│         └─── Application Insights Analysis
│
├─── 9. PRODUCTION DEPLOYMENT
│    ├─── Web APIs
│    │    ├─── Developer UI (16-DevUI)
│    │    ├─── Agent API (23-AgentApp-Api)
│    │    │    ├─── AIAgentFactory
│    │    │    ├─── Middleware
│    │    │    └─── RESTful Endpoints
│    │    │
│    │    └─── Remote MCP API (19-AgentAsRemoteMCP)
│    │
│    ├─── Full Applications
│    │    └─── Chat App (22-ChatApp)
│    │         ├─── Blazor UI
│    │         ├─── .NET Aspire
│    │         ├─── Service Defaults
│    │         ├─── Web API Backend
│    │         └─── Real-time Chat
│    │
│    ├─── Serverless
│    │    └─── Durable Agents (27-MyDurableAgent)
│    │         ├─── Azure Functions
│    │         ├─── Durable Orchestration
│    │         ├─── Persistent Threads
│    │         ├─── Azure Developer CLI (azd)
│    │         ├─── Bicep IaC
│    │         └─── Managed Identity
│    │
│    ├─── Containerization
│    │    ├─── Docker Compose
│    │    └─── Persistent Storage (20-PersistentStorage)
│    │
│    └─── Security
│         ├─── User Secrets
│         ├─── Managed Identity
│         ├─── Security Guards
│         └─── Approval Workflows
│
├─── 10. CONFIGURATION & PATTERNS
│    ├─── Configuration Approaches
│    │    ├─── Code-based Configuration
│    │    ├─── YAML Configuration (24-AIAgent_YAML)
│    │    ├─── appsettings.json
│    │    └─── User Secrets
│    │
│    ├─── Design Patterns
│    │    ├─── Factory Pattern (AIAgentFactory)
│    │    ├─── Builder Pattern (AsBuilder)
│    │    ├─── Middleware Pattern
│    │    ├─── Delegation Pattern
│    │    └─── Agent-as-Service
│    │
│    └─── Extensibility
│         ├─── Custom Tools
│         ├─── Service Injection
│         ├─── Client Factory
│         ├─── AIContextProviderFactory
│         └─── ChatMessageStoreFactory
│
└─── 11. CORE TECHNOLOGIES
     ├─── Frameworks
     │    ├─── Microsoft.Agents.AI
     │    ├─── Microsoft.Agents.AI.OpenAI
     │    ├─── Microsoft.Agents.AI.Workflows
     │    ├─── Microsoft.Extensions.AI
     │    └─── Microsoft.Extensions.VectorData
     │
     ├─── Runtime & Platform
     │    ├─── .NET 9.0
     │    ├─── ASP.NET Core
     │    ├─── Azure Functions
     │    ├─── Blazor
     │    └─── .NET Aspire
     │
     ├─── AI Services
     │    ├─── Azure OpenAI
     │    ├─── Azure AI Search
     │    ├─── Azure AI Foundry
     │    └─── OpenAI API
     │
     ├─── Data & Storage
     │    ├─── SQL Server 2025 (Vector)
     │    ├─── Cosmos NoSQL
     │    ├─── Azure AI Search
     │    └─── InMemory Vector Store
     │
     └─── Supporting Libraries
          ├─── OpenTelemetry
          ├─── Azure.Monitor.OpenTelemetry
          ├─── System.Text.Json
          ├─── ToonNet
          └─── YAML Parsers
```

---

## Visual Hierarchy

### Level 1: Core Concepts (Foundation)
- Agent Creation & Configuration
- LLM Provider Integration
- Response Handling

### Level 2: Enhanced Capabilities
- Tool Calling & Functions
- State Management
- Token Management

### Level 3: Advanced Features
- Multi-Agent Systems
- RAG Implementation
- Multimodal Processing

### Level 4: Production Features
- Observability & Monitoring
- Workflow Orchestration
- Reasoning Control

### Level 5: Enterprise Deployment
- Web APIs & Services
- Durable Agents
- Security & Authentication

---

## Learning Progression Map

```
START HERE
    ↓
[01] Basic Agent Creation
    ↓
[02] Configuration & Settings
    ↓
[05] Token Usage Understanding
    ↓
[03] State Management
    ↓
    ├─→ [04] Tool Calling ──→ [07] Multi-Agent ──→ [10] Workflows
    │                              ↓
    ├─→ [06] Structured Output ────┘
    │
    ├─→ [08] Multimodal ──→ [11] Basic RAG ──→ [12] Advanced RAG
    │
    ├─→ [14] Reasoning ──→ [17] Optimization
    │
    └─→ [13] Observability
            ↓
    [16] Web API ──→ [22] Full App ──→ [27] Durable Agents
            ↓
    [23] Production API
```

---

## Feature Matrix

| Project | Agents | Tools | RAG | Workflows | Multimodal | Production |
|---------|--------|-------|-----|-----------|------------|------------|
| 01 | ✓ | - | - | - | - | - |
| 02 | ✓ | - | - | - | - | - |
| 03 | ✓ | - | - | - | - | - |
| 04 | ✓ | ✓ | - | - | - | - |
| 05 | ✓ | - | - | - | - | - |
| 06 | ✓ | - | - | - | - | - |
| 07 | ✓✓ | ✓ | - | - | - | - |
| 08 | ✓ | - | - | - | ✓ | - |
| 09 | ✓✓ | - | - | - | - | - |
| 10 | ✓✓ | - | - | ✓ | - | - |
| 11 | ✓ | ✓ | ✓ | - | - | - |
| 12 | ✓ | ✓ | ✓✓ | - | - | - |
| 13 | ✓ | - | - | - | - | ✓ |
| 14 | ✓ | - | - | - | - | - |
| 15 | ✓ | ✓ | - | - | - | - |
| 16 | ✓ | - | - | - | - | ✓ |
| 17 | ✓ | ✓ | - | - | - | - |
| 18 | ✓✓ | - | - | - | - | - |
| 19 | ✓ | ✓ | - | - | - | ✓ |
| 20 | ✓ | - | - | - | - | ✓ |
| 21 | ✓ | - | - | - | - | - |
| 22 | ✓ | - | - | - | - | ✓✓ |
| 23 | ✓ | - | - | - | - | ✓✓ |
| 24 | ✓ | - | - | - | - | - |
| 25 | ✓ | ✓ | - | - | - | - |
| 26 | ✓ | - | - | ✓ | - | - |
| 27 | ✓ | - | - | - | - | ✓✓ |

Legend: ✓ = Basic, ✓✓ = Advanced, - = Not Covered

---

## Concept Relationships

```
Agent Creation
    ├─ requires → Configuration
    ├─ produces → Responses
    └─ uses → LLM Providers

Tool Calling
    ├─ extends → Agent Capabilities
    ├─ enables → External Interactions
    └─ requires → Function Definitions

Multi-Agent Systems
    ├─ composed of → Multiple Agents
    ├─ requires → Orchestration
    └─ uses → Workflows

RAG
    ├─ requires → Vector Stores
    ├─ uses → Embeddings
    └─ enhances → Agent Knowledge

Workflows
    ├─ orchestrates → Agent Execution
    ├─ supports → Sequential/Concurrent/Handoff
    └─ enables → Complex Scenarios

Production Deployment
    ├─ requires → Observability
    ├─ uses → Web APIs
    └─ needs → Security
```

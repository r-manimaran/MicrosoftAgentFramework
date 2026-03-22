# Agent Skills — Microsoft Agents AI SDK

## What is a Skill?

A **Skill** is a self-contained, file-based instruction set that tells an AI agent *how* to handle a specific topic or task. Each skill lives in its own folder and is defined by a `SKILL.md` file.

```
skills/
└── my-skill/
    ├── SKILL.md          ← required: name, description, instructions
    ├── references/       ← optional: supporting docs the agent can read
    ├── assets/           ← optional: templates, forms
    └── scripts/          ← optional: executable scripts
```

The `SKILL.md` front-matter tells the agent *when* to activate the skill (`description`), and the body tells it *what to do* (`instructions`). The `FileAgentSkillsProvider` scans a directory, discovers all `SKILL.md` files, and injects the relevant skill into the agent's context at runtime — no code changes needed to add or update a skill.

---

## Project 1 — ConsoleApp

A general-purpose interactive chat agent that demonstrates three different skill types: document lookup, Python script execution, and persona adoption.

### Skills

| Skill | What it does |
|---|---|
| `employee-handbook` | Answers HR questions by referencing Benefits, Pay, Culture, and Attendance docs |
| `secret-formulas` | Executes a Python script via the `execute_python` tool |
| `speak-like-a-pirate` | Adopts the persona of pirate "Seadog John" for kid-friendly responses |

### How it works

1. `FileAgentSkillsProvider` loads all skills from `TestData\AgentSkills\`.
2. A `PythonRunner` tool (`execute_python`) is registered so the agent can run `.py` scripts.
3. `ToolCallingMiddleware` handles the tool-call loop automatically.
4. `Utils.RunChatLoopWithSession` starts an interactive console session.

### Setup & Run

**Prerequisites:** .NET 10, Python installed and on `PATH`, Azure OpenAI access.

1. Configure Azure OpenAI credentials via User Secrets:
   ```bash
   dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
   dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-api-key>"
   ```
2. Run the project:
   ```bash
   cd ConsoleApp
   dotnet run
   ```
3. Try these sample prompts:
   - `What are the company leave benefits?`
   - `Run the secret formula script`
   - `Talk to me like a pirate`

### Adding a New Skill

Create a folder under `TestData\AgentSkills\` with a `SKILL.md`:

```markdown
---
name: my-new-skill
description: Handles questions about <topic>
---

## Instructions
1. Step one...
2. Step two...
```

Mark the file as `CopyToOutputDirectory` in `ConsoleApp.csproj` — no code changes required.

---

## Project 2 — ITHelpDeskAgent

A production-style IT HelpDesk agent for Contoso that handles the three most common employee IT requests using structured, policy-driven skills.

### Skills

| Skill | Trigger scenario | Key resources |
|---|---|---|
| `password-reset` | Account lockout, forgotten password, MFA issues | `SECURITY_POLICY.md`, self-service portal URL |
| `vpn-troubleshooting` | VPN not connecting, slow speeds, certificate errors | `VPN_SERVERS.md` (region endpoints) |
| `software-request` | Install software, license upgrade requests | `REQUEST_FORM_TEMPLATE.md`, approved catalog |

### How it works

1. `FileAgentSkillsProvider` scans the `skills/` directory at startup.
2. The agent is created with a system prompt that sets the Contoso HelpDesk persona.
3. Responses are streamed chunk-by-chunk for a responsive UX.
4. A persistent `AgentSession` maintains conversation context across turns.

### Setup & Run

**Prerequisites:** .NET 10, Azure OpenAI access.

1. Configure Azure OpenAI credentials via User Secrets or environment variables:
   ```bash
   dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
   dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-api-key>"
   ```
2. Run the project:
   ```bash
   cd ITHelpDeskAgent
   dotnet run
   ```
3. Try these sample prompts:
   - `I've been locked out of my account — wrong password too many times.`
   - `My VPN keeps dropping on macOS, I've already tried restarting.`
   - `I need to install Postman for API testing, how do I request it?`

### Adding a New Skill

Drop a new folder under `skills/` — no code changes needed:

```
skills/
└── hardware-request/
    ├── SKILL.md
    └── references/
        └── HARDWARE_CATALOG.md
```

The `skills\**\*` glob in `ITHelpDeskAgent.csproj` copies everything automatically.

---

## Potential Enhancements for Real-World Usage

### Reliability & Quality
- **Skill versioning** — add `version` to `SKILL.md` front-matter and log which version handled each request for auditability.
- **Fallback / escalation skill** — a catch-all skill that creates a ServiceNow/Jira ticket when no other skill matches.
- **Confidence threshold** — reject or escalate responses below a confidence score to avoid hallucinated answers.

### Integration
- **Ticketing system integration** — auto-create tickets in ServiceNow or Jira directly from the agent instead of instructing users to do it manually.
- **Active Directory / LDAP lookup** — verify employee identity and fetch account status before guiding through password reset.
- **Email / Teams notifications** — send confirmation emails or Teams messages after a software request is submitted.

### Security & Compliance
- **Role-based skill access** — restrict sensitive skills (e.g., password-reset) to authenticated users only.
- **Audit logging** — persist every conversation turn with user ID, skill used, and timestamp to a database for compliance.
- **PII redaction** — strip employee IDs and emails from logs before storage.

### Performance & Scalability
- **Skill caching** — cache parsed `SKILL.md` content in memory so the file system is not hit on every request.
- **Async Python execution** — replace the synchronous `Process.WaitForExit()` in `PythonRunner` with async process handling to avoid thread blocking.
- **Web API / Bot Framework host** — expose the agent as an HTTP endpoint so it can be embedded in Teams, Slack, or a web portal instead of running as a console app.

### Developer Experience
- **Skill unit tests** — validate each `SKILL.md` parses correctly and the agent routes sample queries to the expected skill.
- **Hot-reload skills** — use a `FileSystemWatcher` to reload skills without restarting the application.
- **Skill authoring CLI** — a small tool to scaffold a new skill folder with the correct front-matter template.

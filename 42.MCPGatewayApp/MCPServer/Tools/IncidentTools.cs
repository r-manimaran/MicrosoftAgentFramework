using MCPServer.Data;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MCPServer.Tools;

/// <summary>
/// MCP tool class for IT Support incident mangement.
/// 
/// /// Three tools are exposed:
///   1. get_open_incidents  — list active tickets with optional filters
///   2. get_ticket_detail   — full detail + audit history for one ticket
///   3. update_ticket_status — change status and write audit trail
///
/// Rules:
///   • Each [McpServerTool] method must be async Task&lt;string&gt; — the MCP SDK
///     serialises the return value as a text content block for the agent.
///   • [Description] on the class and every parameter feeds the tool manifest
///     that the agent uses to decide which tool to call and how to call it.
///   • Keep descriptions precise — vague descriptions cause wrong tool selection.
/// </summary>

[McpServerToolType]
[Description("IT support ticket management tools for querying and updating incidents")]
public class IncidentTools
{
    private readonly TicketRepository _repo;
    private readonly ILogger<IncidentTools> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    public IncidentTools(TicketRepository repo, ILogger<IncidentTools> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // --- Tool 1: get_open_incidents
    [McpServerTool(Name = "get_open_incidents")]
    [Description("""
        Returns all currently open or in-progress IT support tickets.
        Optionally filter by priority (P1, P2, P3, P4) and/or assignment group.
        Results are ordered by priority (P1 first) then creation time.
        Use this to get a current overview of active incidents.
        """)]
    public async Task<string> GetOpenIncidents(
        [Description("Priority filter: P1 (critical), P2 (high), P3 (medium), P4 (low). Omit for all priorities.")]
        string? priority = null,

        [Description("Team filter e.g. 'Network', 'DevOps', 'EndUser', 'Backend'. Omit for all teams.")]
        string? team = null,

        CancellationToken ct = default)
    {
        _logger.LogInformation("[TOOL] get_open_incidents priority={Priority} team={Team}", priority, team);

        var tickets = await _repo.GetOpenTicketsAsync(priority, team, ct);

        if (tickets.Count == 0)
        {
            var filter = BuildFilterDescription(priority, team);
            return $"No open incidents found{filter}.";
        }

        var result = new
        {
            count = tickets.Count,
            filters = new { priority, team },
            tickets
        };

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    // ─── Tool 2: get_ticket_detail ────────────────────────────────────────────

    [McpServerTool(Name = "get_ticket_detail")]
    [Description("""
        Returns full details for a single ticket including description, current status,
        assigned team/person, and complete audit history of all changes.
        Use this when you need to understand the full context of a specific incident.
        """)]
    public async Task<string> GetTicketDetail(
        [Description("The numeric ticket ID (e.g. 42). Obtain from get_open_incidents first.")]
        int ticketId,

        CancellationToken ct = default)
    {
        _logger.LogInformation("[TOOL] get_ticket_detail ticketId={Id}", ticketId);

        var detail = await _repo.GetTicketByIdAsync(ticketId, ct);

        if (detail is null)
            return $"Ticket #{ticketId} not found. Use get_open_incidents to find valid ticket IDs.";

        return JsonSerializer.Serialize(detail, JsonOpts);
    }

    // ─── Tool 3: update_ticket_status ─────────────────────────────────────────

    [McpServerTool(Name = "update_ticket_status")]
    [Description("""
        Updates the status of an existing ticket and records an audit entry.
        Valid status values: Open, InProgress, PendingUser, Resolved, Closed.
        When setting to Resolved, include resolution notes explaining the fix.
        Always provide a meaningful notes value — it becomes the permanent audit record.
        """)]
    public async Task<string> UpdateTicketStatus(
        [Description("The numeric ticket ID to update.")]
        int ticketId,

        [Description("New status: Open | InProgress | PendingUser | Resolved | Closed")]
        string newStatus,

        [Description("Your name or email address — recorded in the audit trail.")]
        string changedBy,

        [Description("Explain what changed and why. Required for Resolved — describe the fix.")]
        string notes,

        CancellationToken ct = default)
    {
        _logger.LogInformation("[TOOL] update_ticket_status ticketId={Id} newStatus={Status} by={User}",
            ticketId, newStatus, changedBy);

        try
        {
            var updated = await _repo.UpdateTicketStatusAsync(ticketId, newStatus, changedBy, notes, ct);

            if (updated is null)
                return $"Ticket #{ticketId} not found. Cannot update.";

            var result = new
            {
                success = true,
                message = $"Ticket #{ticketId} updated to {newStatus}",
                ticket = updated
            };

            return JsonSerializer.Serialize(result, JsonOpts);
        }
        catch (ArgumentException ex)
        {
            return $"Validation error: {ex.Message}";
        }
    }

    // --Helper methods
    private static string BuildFilterDescription(string? priority, string? assignmentGroup)
    {
        var parts = new List<string>();
        if (priority is not null) parts.Add($"priority={priority}");
        if (assignmentGroup is not null) parts.Add($"assignmentGroup={assignmentGroup}");
        return parts.Count > 0 ? $" matching {string.Join(", ", parts)}" : "";
    }
}

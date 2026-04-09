using McpGateway.Core.Interfaces;
using McpGateway.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Registry;

public sealed class InMemoryMcpServerRegistry : IMcpServerRegistry
{
    private readonly Dictionary<string,McpServerRegistration> _servers;

    public InMemoryMcpServerRegistry(IEnumerable<McpServerRegistration> registrations)
    {
       _servers = registrations.ToDictionary(r=>r.ServerId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns null if the serverId is not registered. 
    /// This is a simple in-memory implementation, so it does not support wildcard or pattern matching. 
    /// The serverId must match exactly.
    /// </summary>
    /// <returns></returns>
    public McpServerRegistration? Resolve(string serverId)
    => _servers.GetValueOrDefault(serverId);

    public IReadOnlyList<McpServerRegistration> All()
     => _servers.Values.ToList().AsReadOnly();


}

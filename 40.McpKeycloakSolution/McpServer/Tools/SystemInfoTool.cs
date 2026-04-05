using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Tools;

[McpServerToolType]
public static class SystemInfoTool
{
    [McpServerTool, Description("Get server system info (admin only)")]
    [Authorize(Policy = "McpAdmin")]
    public static object GetSystemInfo()
    {
        return new
        {
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.ToString(),
            DotnetVersion = Environment.Version.ToString(),
            Uptime = Environment.TickCount64 / 1000,
        };
    }
}

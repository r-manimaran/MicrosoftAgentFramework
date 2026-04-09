using McpGateway.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Interfaces;

public interface IMcpMiddleware
{
    Task InvokeAsync(McpGatewayContext context, Func<McpGatewayContext, Task> next);
}

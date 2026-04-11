using System;
using System.Collections.Generic;
using System.Text;

namespace McpAgentApp.Models;

internal record GatewayServerInfo(
    string ServerId,
    string BaseUrl,
    string Transport,
    string[] AllowedTools,
    int TimeoutSeconds);
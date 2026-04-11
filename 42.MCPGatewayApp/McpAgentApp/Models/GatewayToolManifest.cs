using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace McpAgentApp.Models;
internal record GatewayToolManifest(
    string Name,
    string Description,
    JsonElement InputSchema);
using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentApp;

public class AIAgentResponseEvent(string response): WorkflowEvent(response)
{
    public override string ToString() => response;
    
}

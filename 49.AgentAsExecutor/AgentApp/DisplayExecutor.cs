using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentApp;

public class DisplayExecutor:Executor<string>
{
    public DisplayExecutor() :base("DisplayExecutor")
    {
        
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
       await context.AddEventAsync(new AIAgentResponseEvent(message), cancellationToken);
       
        return;
    }
}

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentApp;

public class AgentExecutor : Executor
{
    private readonly AIAgent _agent;
    public AgentExecutor(string id, AIAgent agent):base(id)
    {
        _agent = agent;
    }


    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
    {
        var res = await _agent.RunAsync(message, cancellationToken: ct);
        return res.Text;
    }
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        protocolBuilder.ConfigureRoutes(rb => rb.AddHandler<string, string>(HandleAsync));
        return protocolBuilder;
    }
}

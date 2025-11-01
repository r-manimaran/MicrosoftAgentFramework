using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIAgentRAG;

public static class Middleware
{
    public static async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken = default)
    {
        StringBuilder functionCallDetails = new();
        functionCallDetails.Append($" - Tool call :'{context.Function.Name}' [Agent:{callingAgent.Name}]");
        if (context.Arguments.Count > 0)
        {
            functionCallDetails.Append(" with args: ");
            foreach (var arg in context.Arguments)
            {
                functionCallDetails.Append($" {arg.Key}='{arg.Value}' ");
            }
        }
        Utils.WriteLineInformation(functionCallDetails.ToString());
        return await next(context, cancellationToken);
    }
}

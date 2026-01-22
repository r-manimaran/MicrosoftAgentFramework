using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace WebApi;

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
        // Try to resolve an ILogger from the agent's service container (if any).
        ILogger? logger = null;
        try
        {
            var agentType = callingAgent.GetType();
            var servicesProp = agentType.GetProperty("Services") ?? agentType.GetProperty("ServiceProvider");
            var services = servicesProp?.GetValue(callingAgent) as IServiceProvider;
            if (services != null)
            {
                // Prefer typed ILogger<Middleware> then ILoggerFactory fallback
                logger = services.GetService(typeof(ILogger<Program>)) as ILogger<Program>
                         ?? (services.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger("FunctionCallMiddleware");
            }
        }
        catch
        {
            // Swallow any resolution exceptions and fall back to Console
        }

        if (logger != null)
        {
            logger.LogInformation(functionCallDetails.ToString());
        }
        else
        {
            Console.WriteLine(functionCallDetails.ToString());
        }

        //Utils.WriteLineInformation(functionCallDetails.ToString());
        return await next(context, cancellationToken);
    }
}
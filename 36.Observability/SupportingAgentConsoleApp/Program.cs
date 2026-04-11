
using Azure.AI.OpenAI;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SupportingAgentConsoleApp;
using SupportingAgentConsoleApp.Alerting;
using SupportingAgentConsoleApp.Config;
using SupportingAgentConsoleApp.Evaluation;
using SupportingAgentConsoleApp.Evaluation.Scorers;
using SupportingAgentConsoleApp.Extensions;
using SupportingAgentConsoleApp.Helpers;
using SupportingAgentConsoleApp.Middleware;
using SupportingAgentConsoleApp.Telemetry;
using System.ClientModel;
using System.Text;
using System.Threading;


// Adding OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
                            .AddService(serviceName: "support-agent-console", serviceVersion: "1.0.0");
var tracerBuilder = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(AgentTelemetry.SourceName)
    .AddHttpClientInstrumentation()
    .AddConsoleExporter();

if (!string.IsNullOrWhiteSpace(LLMConfig.ApplicationInsightsConnectionString))
    tracerBuilder.AddAzureMonitorTraceExporter(
        o => o.ConnectionString = LLMConfig.ApplicationInsightsConnectionString);

// -- Seq
OtlpTraceExporter seqTraceExporter = null;
if(!string.IsNullOrWhiteSpace(LLMConfig.SeqServerUrl))
{
   seqTraceExporter = new OtlpTraceExporter(new OtlpExporterOptions
    {
        Endpoint = new Uri($"{LLMConfig.SeqServerUrl}/ingest/otlp/v1/traces"),
        Protocol = OtlpExportProtocol.HttpProtobuf,
        Headers = string.IsNullOrWhiteSpace(LLMConfig.SeqApiKey)
                    ? string.Empty
                    : $"X-Seq-ApiKey={LLMConfig.SeqApiKey}",
    });
    
}
using TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
                  .SetResourceBuilder(resourceBuilder)
                  .AddSource(AgentTelemetry.SourceName)
                  .AddHttpClientInstrumentation()
                  .AddConsoleExporter()
                  .AddProcessor(new SimpleActivityExportProcessor(seqTraceExporter))
                  .Build()!;
// using TracerProvider tracerProvider = tracerBuilder.Build();

var meterBuilder = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter(AgentTelemetry.SourceName)
    .AddConsoleExporter();

if (!string.IsNullOrWhiteSpace(LLMConfig.ApplicationInsightsConnectionString))
    meterBuilder.AddAzureMonitorMetricExporter(
        o => o.ConnectionString = LLMConfig.ApplicationInsightsConnectionString);

// -- Seq
OtlpTraceExporter seqMetricExporter =null;
if (!string.IsNullOrWhiteSpace(LLMConfig.SeqServerUrl))
{

    seqMetricExporter = new OtlpTraceExporter(new OtlpExporterOptions
    {
        Endpoint = new Uri($"{LLMConfig.SeqServerUrl}/ingest/otlp/v1/metrics"),
        Protocol = OtlpExportProtocol.HttpProtobuf,
        Headers = string.IsNullOrWhiteSpace(LLMConfig.SeqApiKey)
                   ? string.Empty
                   : $"X-Seq-ApiKey={LLMConfig.SeqApiKey}",
    });

  
}
using TracerProvider metricsProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource(AgentTelemetry.SourceName)
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
                .AddProcessor(new SimpleActivityExportProcessor(seqMetricExporter))
                .Build()!;


//using MeterProvider meterProvider = meterBuilder.Build();

Console.WriteLine($"  Seq        : {(string.IsNullOrWhiteSpace(LLMConfig.SeqServerUrl) ? "disabled" : LLMConfig.SeqServerUrl)}");

// --------------------------------
// Alert dispatcher options - read from environment variables
var alertOptions = new AlertDispatcherOptions
{
    // Score thresholds - lower than these triggers an alert
    SafetyThreshold = double.Parse(Environment.GetEnvironmentVariable("ALERT_SAFETY_THRESHOLD") ?? "0.7"),
    HallucinationThreshold = double.Parse(Environment.GetEnvironmentVariable("ALERT_HALLUCINATION_THRESHOLD") ?? "0.5"),
    RelevanceThreshold = double.Parse(Environment.GetEnvironmentVariable("ALERT_RELEVANCE_THRESHOLD") ?? "0.6"),

    // Cooldown — prevents the same dimension alerting more than once per window
    CooldownWindow = TimeSpan.FromMinutes(
        int.Parse(Environment.GetEnvironmentVariable("ALERT_COOLDOWN_MINUTES") ?? "5")),

    // Seq span events — always on if OTel is configured
    SeqEnabled = true,

    // Webhook — set ALERT_WEBHOOK_URL to enable (leave empty to skip)
    WebhookUrl = Environment.GetEnvironmentVariable("ALERT_WEBHOOK_URL") ?? string.Empty,
    WebhookFormat = Enum.Parse<WebhookFormat>(
                        Environment.GetEnvironmentVariable("ALERT_WEBHOOK_FORMAT") ?? "Generic",
                        ignoreCase: true),

    // Only needed when WebhookFormat = PagerDuty
    PagerDutyRoutingKey = Environment.GetEnvironmentVariable("PAGERDUTY_ROUTING_KEY") ?? string.Empty,
};

var alertDispatcher = new AlertDispatcher(alertOptions);
Console.WriteLine($"[Init] AlertDispatcher ready.");
Console.WriteLine($" Safety threshold       : {alertOptions.SafetyThreshold}");
Console.WriteLine($" Hallucination threshold: {alertOptions.HallucinationThreshold}");
Console.WriteLine($" Relevance threshold    : {alertOptions.RelevanceThreshold}");
Console.WriteLine($" Cooldown window        : {alertOptions.CooldownWindow.TotalMinutes} min");
Console.WriteLine($" Webhook                : {(string.IsNullOrWhiteSpace(alertOptions.WebhookUrl) ? "disabled" : alertOptions.WebhookFormat.ToString())}");


// -- Dependency Injection
PromptLogger? promptLogger = null;
if (!string.IsNullOrWhiteSpace(LLMConfig.BlobConnectionString))
{
    promptLogger = new PromptLogger(LLMConfig.BlobConnectionString);
    Console.WriteLine("[Init] PromptLogger initialized with Blob Storage.");
}
else
{
    Console.WriteLine("[Init] PromptLogger skipped — AZURE_BLOB_CONNECTION_STRING not set.");
}

// - Evaulation pipeline with two scorers: relevance and hallucination
EvalPipeline? evalPipeline = null;
if (!string.IsNullOrWhiteSpace(LLMConfig.EvalStoreConnectionString))
{
    var relevanceScorer = new RelevanceScorer(LLMConfig.Endpoint, LLMConfig.ApiKey, LLMConfig.JudgeModel);
    var hallucinationScorer = new HallucinationScorer(LLMConfig.Endpoint, LLMConfig.ApiKey, LLMConfig.JudgeModel);
    var safetyScorer = new SafetyScorer(LLMConfig.Endpoint, LLMConfig.ApiKey, LLMConfig.JudgeModel);

    evalPipeline = new EvalPipeline(relevanceScorer, hallucinationScorer, safetyScorer, alertDispatcher, LLMConfig.EvalStoreConnectionString);

    Console.WriteLine("[Init] EvalPipeline initialized with all 3 scorers.");
}
else
{
    Console.WriteLine("[Init] EvalPipeline skipped (no eval store) — EVAL_STORE_CONNECTION_STRING not set.");
}

// -- Cost Tracker -emits token usage as OTel metrics; always active
var costTracker = new CostTracker(LLMConfig.InputCostPer1kTokens, LLMConfig.OutputCostPer1kTokens);
Console.WriteLine("[Init] CostTracker ready.");

// 1. Create a Agent
var client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint), new ApiKeyCredential(LLMConfig.ApiKey));

AIAgent supportAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).AsAIAgent(
    name: "SupportAgent",
    instructions: """
    You are a helpful SaaS support agent.
    Answer clearly and concisely based only on verified product information.
    If you are unsure about something, say so — do not guess or fabricate.
    Keep responses focused and actionable.
    """)
    .AsBuilder()
    .UseOpenTelemetry(AgentTelemetry.SourceName, o =>
    {
        // true here because this is a local dev/exploration console host
        // Set to false (or read from env var) when pointing at real customers
        o.EnableSensitiveData = true;
    })
    .Use(FunctionCallMiddleware)
    .Build();

Console.WriteLine("[Init] Agent build successfully");
Console.WriteLine();

// 5. Service Construction
var logger = new SupportingAgentConsoleApp.ConsoleLogger<SupportAgentService>();
var service = new SupportAgentService(supportAgent, promptLogger!, evalPipeline!, costTracker, logger);

// 2. Create a Conversation loop
AgentSession session = await supportAgent.CreateSessionAsync();
string sessionId = Guid.NewGuid().ToString("N")[..8];  // short ID for display
string userId = "dev-user-001"; // In a real app, this would come from auth context
string productLine = "platform-core";
int turnNumber = 0;


Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine($"  Session: {sessionId}  |  Type your message and press Enter.");
Console.WriteLine("  Type 'exit' or press Ctrl+C to end the session.");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

using var cts = new CancellationTokenSource();

// Catch Ctr+C so we can flush
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n[Session] Ctrl +C received - shutting down gracefully..");
};

while (!cts.IsCancellationRequested)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("You: ");
    Console.ResetColor();

    string? input = Console.ReadLine();

    if (input == null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("[Session] Ending session.");
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
        continue;
    turnNumber++;
    // Each turn gets a unique ticket ID so traces are correlated per message
    string ticketId = $"{sessionId}-turn{turnNumber:D3}";

    try
    {
        /* AgentResponse response = await supportAgent.RunAsync(input, session);
         Console.WriteLine(response);
         response.Usage.OutputAsInformation();*/
        string response = await service.HandleTicketAsync(
             ticketId: ticketId,
             userId: userId,
             message: input,
             productLine: productLine,
             session: session,
             ct: cts.Token);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Agent: ");
        Console.ResetColor();
        Console.WriteLine(response);
        Console.WriteLine();
    }
    catch (OperationCanceledException)
    {
        // Ctrl+C mid-request -exit cleanly
        break;
    }
    catch(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Error] {ex.GetType().Name}: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}

Console.WriteLine();
Console.WriteLine("[Shutdown] Flushing telemetry...");
// TracerProvider and MeterProvider flush on Dispose — the `using` declarations
// at the top of Program.cs guarantee this happens before process exit.
Console.WriteLine("[Shutdown] Done. Goodbye.");

async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context,
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
    Utils.WriteLine(functionCallDetails.ToString(), ConsoleColor.DarkGray);
    return await next(context, cancellationToken);
}
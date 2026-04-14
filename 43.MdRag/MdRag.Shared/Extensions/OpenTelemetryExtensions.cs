using MdRag.Shared.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace MdRag.Shared.Extensions;

/// <summary>
/// Registers OpenTelemetry tracing, metrics, and structured logging for any
/// MdRag service. Call this once in each project's Program.cs.
///
/// Usage:
///   builder.AddRagObservability("MdRag.Api");
///
/// In local dev, Aspire automatically injects OTEL_EXPORTER_OTLP_ENDPOINT and
/// OTEL_SERVICE_NAME, so the OTLP exporter sends everything to the Aspire dashboard
/// without any manual configuration.
///
/// In production, set OTEL_EXPORTER_OTLP_ENDPOINT to your Application Insights
/// or Grafana OTLP ingest endpoint. Key Vault / Container Apps environment variables
/// handle secret injection.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder AddRagObservability(this IHostApplicationBuilder builder,
        string serviceName)
    {
        //------------------------------------------
        // Shared resource attributes - appear on every span, metric and log
        var resourceBuilder = ResourceBuilder
                        .CreateDefault()
                        .AddService(serviceName)
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName,
                        });


        //----------------------------
        // Tracing
        //----------------------------
        builder.Services
                .AddOpenTelemetry()
                .WithTracing(tracing => tracing.SetResourceBuilder(resourceBuilder)

                // Built-in instrumentation
                .AddAspNetCoreInstrumentation(opts =>
                {
                    // Record request bodies for internal health-check exclusion
                    opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation(opts =>
                {
                    // supress noisy Azure SDK polling spans
                    opts.FilterHttpRequestMessage = req =>
                        req.RequestUri?.Host.Contains("azure.com") != true
                        || req.RequestUri.AbsolutePath.Contains("openai");
                })
                .AddEntityFrameworkCoreInstrumentation(opts =>
                {
                    opts.SetDbStatementForText = true; // captures SQL query text
                })

                // Custom RAG activity sources
                .AddSource(ActivitySources.AllSourceNames)

                // OLTP export- endpoint injected by Aspire or environment variable
                .AddOtlpExporter())

            //--------------------------------
            // Metrics
            // -------------------------------
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)

             // Built-in instrumentation
             .AddAspNetCoreInstrumentation()
             .AddHttpClientInstrumentation()
             .AddRuntimeInstrumentation() // GC, thread pool, memory

             // custom RAG meter
             .AddMeter(RagMeters.MeterName)

             // OTLP export
             .AddOtlpExporter()
             );

        //------------------------
        // Strunctured logging via serilog
        // (SourceContext, ThreadId, MachineName) that make filtering much easier.
        //------------------------------------------
        builder.Logging.ClearProviders();

        Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
           .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
           .MinimumLevel.Override("System", LogEventLevel.Warning)
           .Enrich.FromLogContext()
           .Enrich.WithMachineName()
           .Enrich.WithThreadId()
           .Enrich.WithProperty("ServiceName", serviceName)
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj} {Properties}{NewLine}{Exception}")
           .CreateLogger();

        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        // Wire OTel logging pipeline so Aspire dashboard receives log records too
        builder.Services
            .AddOpenTelemetry()
            .WithLogging(logging => logging
                .SetResourceBuilder(resourceBuilder)
                .AddOtlpExporter());

        return builder;

    }
}

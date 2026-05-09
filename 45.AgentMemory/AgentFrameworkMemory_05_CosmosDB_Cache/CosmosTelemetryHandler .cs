using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AgentWithMemory_06_CosmosDB_Cache;

public class CosmosTelemetryHandler : RequestHandler
{
    private readonly ILogger _logger;

    public CosmosTelemetryHandler(ILogger logger)
    {
        _logger = logger;
    }

    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid().ToString("N")[..8];

        // ── Capture Request ──────────────────────────────────
        var resourceType = request.GetType().Name;
        var method = request.Method.Method;
        var requestUri = request.RequestUri;

        _logger.LogInformation(
            "[Cosmos][{Id}] ➡ {Method} {ResourceType} | URI: {Uri}",
            operationId, method, resourceType, requestUri);

        // ── Call next handler ────────────────────────────────
        ResponseMessage response = await base.SendAsync(request, cancellationToken);

        stopwatch.Stop();

        // ── Capture Response ─────────────────────────────────
        var status = (int)response.StatusCode;
        var ru = response.Headers.RequestCharge;

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "[Cosmos][{Id}] ✅ {Method} {ResourceType} | Status: {Status} | RU: {RU} | Time: {Ms}ms",
                operationId, method, resourceType, status, ru, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogError(
                "[Cosmos][{Id}] ❌ {Method} {ResourceType} | Status: {Status} | RU: {RU} | Time: {Ms}ms | Error: {Error}",
                operationId, method, resourceType, status, ru, stopwatch.ElapsedMilliseconds,
                response.ErrorMessage);
        }

        return response;
    }

    
}

public static class CosmosLogging
{
    // Helper to trace cache operations
    public static async Task<T?> TraceAsync<T>(string operation, Func<Task<T?>> action, ILogger logger)
    {
        using var activity = new Activity(operation).Start();
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            logger.LogInformation(
                "[Trace] {Operation} completed in {Ms}ms | Hit: {Hit}",
                operation, sw.ElapsedMilliseconds, result != null);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Trace] {Operation} failed after {Ms}ms", operation, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

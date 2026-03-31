using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MCPapp.Server.Tools;

/// <summary>
/// Currency conversion using the free exchangerate.host API.
/// Demonstrates: parameterised HTTP call, error handling, decimal math.
/// </summary>
[McpServerToolType]
public sealed class CurrencyTool(IHttpClientFactory httpFactory)
{
    private readonly HttpClient _http = httpFactory.CreateClient();

    [McpServerTool(Name = "convert_currency")]
    [Description("Convert an amount from one currency to another using live exchange rates. " +
                 "Supported codes: USD, EUR, GBP, JPY, CAD, AUD, CHF, INR, etc.")]
    public async Task<CurrencyResult> ConvertAsync(
        [Description("Source currency code, e.g. 'USD'.")] string from,
        [Description("Target currency code, e.g. 'EUR'.")] string to,
        [Description("Amount to convert.")] decimal amount,
        CancellationToken ct = default)
    {
        from = from.ToUpperInvariant();
        to = to.ToUpperInvariant();

        // Frankfurter API — free, no auth
        var url = $"https://api.frankfurter.app/latest?from={from}&to={to}";
        var response = await _http.GetFromJsonAsync<FrankfurterResponse>(url, ct)
                       ?? throw new InvalidOperationException("Empty response from exchange-rate API.");

        if (!response.Rates.TryGetValue(to, out var rate))
            throw new ArgumentException($"Currency '{to}' not supported or unknown.");

        return new CurrencyResult(
            From: from,
            To: to,
            OriginalAmount: amount,
            ConvertedAmount: Math.Round(amount * rate, 4),
            Rate: rate,
            Date: response.Date);
    }
}

public record CurrencyResult(
    string From,
    string To,
    decimal OriginalAmount,
    decimal ConvertedAmount,
    decimal Rate,
    string Date);

internal sealed class FrankfurterResponse
{
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("rates")] public Dictionary<string, decimal> Rates { get; set; } = [];
}

using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace MCPapp.Server.Tools;

/// <summary>
/// Fetches current weather data from the Open-Meteo free API (no API key required)
/// Demonstrates: async HTTP call, structured JSON result, real-world data.
/// </summary>
[McpServerToolType]
public sealed class WeatherTool(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [McpServerTool(Name ="get_weather")]
    [Description("Get current weather for a city latitude/longitude. Returns temperature, wind speed and weather description.")]
    public async Task<WeatherResult> GetWeatherAsync(
        [Description("Latitude of the location (e.g 40.7128 for Newyork)")] double latitude, 
        [Description("Longitude of the location (e.g -74.0060 for New York)")] double longitude, 
         CancellationToken ct = default)
    {
        // Open-Meteo API endpoint for current weather
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={latitude}&longitude={longitude}" +
                  $"&current=temperature_2m,wind_speed_10m,weather_code" +
                  $"&temperature_unit=celsius";

        var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, ct)
                       ?? throw new InvalidOperationException("Empty response from weather API.");

        var current = response.Current;
        
        return new WeatherResult(
            Latitude: latitude,
            Longitude: longitude,
            TemperatureC: current.Temperature2m,
            WindSpeedKmh: current.WindSpeed10m,
            Description: WmoCodeToDescription(current.WeatherCode)
        );
    }

    // WMO weather interpretation codes (subset)
    private static string WmoCodeToDescription(int code) => code switch
    {
        0 => "Clear sky",
        1 or 2 => "Mainly clear / partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow",
        80 or 81 or 82 => "Rain showers",
        95 => "Thunderstorm",
        _ => $"Weather code {code}"
    };
}

public record WeatherResult(
    double Latitude,
    double Longitude,
    double TemperatureC,
    double WindSpeedKmh,
    string Description);

// Minimal deserialization types for Open-Meteo
internal sealed class OpenMeteoResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather Current { get; set; } = new();
}

internal sealed class CurrentWeather
{
    [JsonPropertyName("temperature_2m")] public double Temperature2m { get; set; }
    [JsonPropertyName("wind_speed_10m")] public double WindSpeed10m { get; set; }
    [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
}
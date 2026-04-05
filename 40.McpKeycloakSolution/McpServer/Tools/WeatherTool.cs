using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Tools;

[McpServerToolType]
public static class WeatherTool
{
    [McpServerTool, Description("Get Current weather for a city")]
    public static string GetWeather([Description("City Name")] string city)
    {
        // Replace with real weather Api Call
        return $"Weather in {city}: 22°C, partly cloudy";
    }
}

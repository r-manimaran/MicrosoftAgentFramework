using Microsoft.Extensions.Configuration;

namespace ConsoleAppXAiGrok;

public class GroqAIConfig
{
    private static readonly IConfiguration configuration;

    static GroqAIConfig()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<GroqAIConfig>()
            .Build();
    }

    public static string ApiKey => configuration["GrokAI:ApiKey"] ?? throw new ArgumentNullException("GrokAI:Api is missing in the Configuration");
    public static string Endpoint => configuration["GrokAI:Endpoint"] ?? throw new ArgumentNullException("GrokAI:Endpoint is missing in the Configuration");
    public static string ModelId => configuration["GrokAI:ModelId"] ?? "grok-3-mini";
}

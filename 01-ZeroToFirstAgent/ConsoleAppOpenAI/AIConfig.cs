using Microsoft.Extensions.Configuration;

namespace ConsoleAppOpenAI;

public class AIConfig
{
    private static readonly IConfiguration configuration;
    
    static AIConfig()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<AIConfig>()
            .Build();
    }

    public static string ApiKey => configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:Api");
    public static string ModelId => configuration["OpenAI:ModelId"] ?? "gpt-4o";
}

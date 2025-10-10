using GenerativeAI;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppGemini;

public class GeminiAIConfig
{
    public static readonly IConfiguration configuration;
    static GeminiAIConfig()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<GeminiAIConfig>()
            .Build();
    }

    public static string ApiKey => configuration["GeminiAI:ApiKey"];
    public static string ModelId => configuration["GeminiAI:ModelId"] ?? GoogleAIModels.Gemini25Flash;
}

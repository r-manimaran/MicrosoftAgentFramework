using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

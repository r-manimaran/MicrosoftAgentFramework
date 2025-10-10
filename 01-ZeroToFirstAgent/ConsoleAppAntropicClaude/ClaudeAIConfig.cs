using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppAntropicClaude;

public class ClaudeAIConfig
{
    public static readonly IConfiguration configuration;
    static ClaudeAIConfig()
    {
        configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<ClaudeAIConfig>()
            .Build();
    }

    public static string ApiKey => configuration["ClaudeAI:ApiKey"] ?? throw new ArgumentNullException("ClaudeAI:ApiKey");

}

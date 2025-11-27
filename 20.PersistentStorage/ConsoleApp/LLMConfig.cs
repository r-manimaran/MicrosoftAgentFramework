using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp;

public class LLMConfig
{
    private static readonly IConfiguration configuration;
    static LLMConfig()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<LLMConfig>()
            .Build();
    }
    public static string DeploymentOrModelId => configuration["AzureAI:ModelId"] ?? "gpt-4o";
    public static string Endpoint => configuration["AzureAI:Endpoint"] ?? throw new ArgumentNullException("AzureAI:Endpoint");
    public static string ApiKey => configuration["AzureAI:ApiKey"] ?? throw new ArgumentNullException("AzureAI:Api");
    public static string GitHubToken => configuration["GitHub:Token"] ?? throw new ArgumentNullException("GitHub:Token");

    public static string DocumentDBUserName => configuration["Documentdb:Username"] ?? throw new ArgumentNullException("Documentdb:Username");
    public static string DocumentDBPassword => configuration["Documentdb:Password"] ?? throw new ArgumentNullException("Documentdb:Password");

}

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Config;

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
    public static string OpenWeatherApiKey => configuration["Weather:ApiKey"] ?? throw new ArgumentNullException("Weather:ApiKey");
    public static string ApplicationInsightsConnectionString => configuration.GetConnectionString("AppInsight") ?? throw new ArgumentNullException("Missing App InsightConnection String");
    public static string? BlobConnectionString => configuration.GetConnectionString("BlobStorage");
    public static string? EvalStoreConnectionString => configuration.GetConnectionString("EvalStore");
    public static int InputCostPer1kTokens => int.TryParse(configuration["LLM:InputCostPer1kTokens"], out var inputCost) ? inputCost : 0;
    public static int OutputCostPer1kTokens => int.TryParse(configuration["LLM:OutputCostPer1kTokens"], out var outputCost) ? outputCost : 0;
    public static string JudgeModel => configuration["LLM:JudgeModel"] ?? "gpt-4o-mini";

    //Open Telemetry Seq settings
    public static string SeqServerUrl => configuration["Seq:ServerUrl"] ?? throw new ArgumentNullException("Seq:ServerUrl");
    public static string SeqApiKey => configuration["Seq:ApiKey"] ?? throw new ArgumentNullException("Seq:ApiKey");

}
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentApp;
/// <summary>
/// Provides access to configuration settings required for connecting to Azure AI services and the application's
/// database.
/// </summary>
/// <remarks>This class retrieves configuration values from the 'appSettings.json' file and user secrets at
/// application startup. All properties are static and provide read-only access to their respective configuration
/// values. If a required configuration value is missing, an exception is thrown when accessing the corresponding
/// property.</remarks>
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
    public static string EmbeddingModelName => configuration["AzureAI:EmbeddingModelName"] ?? "text-embedding-3-small";
    public static string SqlConnectionString => configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionStrings:DefaultConnection");
}

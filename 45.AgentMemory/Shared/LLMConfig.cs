using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared;

public class LLMConfig
{
    private static readonly IConfiguration configuration;
    static LLMConfig()
    {
        configuration = new ConfigurationBuilder()
            //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<LLMConfig>()
            .Build();
    }
    public static string DeploymentOrModelId => configuration["AzureAI:ModelId"] ?? "gpt-4o";
    public static string Endpoint => configuration["AzureAI:Endpoint"] ?? throw new ArgumentNullException("AzureAI:Endpoint");
    public static string ApiKey => configuration["AzureAI:ApiKey"] ?? throw new ArgumentNullException("AzureAI:Api");
    public static string EmbeddingDeploymentName => configuration["AzureAI:EmbeddingModel"] ?? throw new ArgumentNullException("AzureAI:EmbeddingModel");
    public static string ConnectionString => configuration["SqlServer:ConnectionString"] ?? throw new ArgumentNullException("SqlServer:ConnectionString");

    public static string MEM0Endpoint => configuration["MEM0:Endpoint"] ?? throw new ArgumentNullException("MEM0_ENDPOINT is not set.");
    public static string MEM0ApiKey => configuration["MEM0:ApiKey"] ?? throw new ArgumentNullException("MEM0 ApiKey not set");
}
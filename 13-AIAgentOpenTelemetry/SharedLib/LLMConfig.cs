﻿using Microsoft.Extensions.Configuration;

namespace SharedLib;

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

    public static string ApplicationInsightsConnectionString => configuration["ApplicationInsights:ConnectionString"] ?? throw new ArgumentNullException("ApplicationInsights:ConnectionString");
}
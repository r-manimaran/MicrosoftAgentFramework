using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentMCPClient;

public class Config
{
    private static readonly IConfiguration configuration;
    static Config()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Config>()
            .Build();
    }
    public static string DeploymentOrModelId => configuration["AzureAI:ModelId"] ?? "gpt-4o";
    public static string Endpoint => configuration["AzureAI:Endpoint"] ?? throw new ArgumentNullException("AzureAI:Endpoint");
    public static string ApiKey => configuration["AzureAI:ApiKey"] ?? throw new ArgumentNullException("AzureAI:Api");
    public static string KeycloakBase => configuration["Keycloak:BaseUrl"] ?? "http://localhost:8081";
    public static string McpServerUrl => configuration["McpServer:BaseUrl"] ?? "http://localhost:5100/mcp";

}

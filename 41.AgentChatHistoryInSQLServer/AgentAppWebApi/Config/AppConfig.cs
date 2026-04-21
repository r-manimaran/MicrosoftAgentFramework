namespace AgentAppWebApi.Config;

public class KeycloakConfig
{
    public string Authority { get; set; } = "";
    public string Audience { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}

public class AzureOpenAIConfig
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ChatDeployment { get; set; } = "";
    public string EmbeddingDeployment { get; set; } = "";
}


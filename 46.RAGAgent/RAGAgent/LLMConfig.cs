using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAGAgent;

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
    public static string ApiKey => configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey");
    public static string ChatModel => configuration["OpenAI:Model"] ?? throw new ArgumentNullException("OpenAI:Model");
    public static string EmbeddingModel => configuration["OpenAI:EmbeddingModel"] ?? throw new ArgumentNullException("OpenAI:EmbeddingModel");


}


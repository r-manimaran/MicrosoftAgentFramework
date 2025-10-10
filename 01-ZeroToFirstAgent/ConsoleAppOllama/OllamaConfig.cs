using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppOllama;

public class OllamaConfig
{
    public static readonly IConfiguration configuration;
    static OllamaConfig()
    {
        configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<OllamaConfig>()
            .Build();
    }

    public static string Endpoint => configuration["Ollama:Endpoint"] ?? throw new ArgumentNullException("Ollama:Endpoint");
    public static string Model => configuration["Ollama:Model"] ?? throw new ArgumentNullException("Ollama:Model");
}

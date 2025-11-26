using Amazon;
using Amazon.BedrockRuntime;
using Anthropic.SDK;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using GenerativeAI.Microsoft;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mistral.SDK;
using OllamaSharp;
using OllamaSharp.Models;
using OpenAI;
using System.ClientModel;


//##### 1. OPENAI
// Package:
OpenAIClient client = new("");
ChatClientAgent agent = client.GetChatClient("").CreateAIAgent();
AgentRunResponse response1 = await agent.RunAsync("Hello");
Console.WriteLine(response1);


//#### 2. Google Gemini
// Package:
IChatClient gClient = new GenerativeAIChatClient("", "");
ChatClientAgent gAgent = new ChatClientAgent(gClient);

AgentRunResponse response2 = await gAgent.RunAsync("Hello");
Console.WriteLine(response2);


//#### 3. Anthropic
// Package:
AnthropicClient anthrophicClient = new AnthropicClient(new Anthropic.SDK.APIAuthentication(""));
IChatClient aClient = anthrophicClient.Messages.AsBuilder().Build();
ChatClientAgent aAgent = new ChatClientAgent(aClient, new ChatClientAgentOptions()
{
    ChatOptions = new ChatOptions
    {
        ModelId = "",
        MaxOutputTokens = 1000
    }
});
AgentRunResponse response3 = await aAgent.RunAsync("Hello");
Console.WriteLine(response3);

//### 4.Mistral
// Package:
MistralClient mistralClient = new MistralClient(new Mistral.SDK.APIAuthentication(""));
AIAgent mAgent = mistralClient.Completions.CreateAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        ModelId = ""
    }
});
AgentRunResponse response4 = await mAgent.RunAsync("Hello");
Console.WriteLine(response4);

//### 5.XAI (Grok)
// Package: Microsoft.Agents.AI.OpenAI
OpenAIClient openAIClient = new(new ApiKeyCredential(""), new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.x.ai/v1")
});
ChatClientAgent xaiAgent = openAIClient.GetChatClient("<YourModelName>").CreateAIAgent();
AgentRunResponse response5 = await xaiAgent.RunAsync("What is the capital of India?");
Console.WriteLine(response5);


//### 6.Azure OpenAI
// Packages:
//  Azure.AI.OpenAI
//  Microsoft.Agents.AI.OpenAI
AzureOpenAIClient azureOpenAIClient = new(new Uri("https://<yourEndpoint>.openai.azure.com/"),
    new ApiKeyCredential("<YourApiKey>"));
ChatClientAgent azureOpenAIAgent = client.GetChatClient("<YourModelDeploymentName>").CreateAIAgent();
AgentRunResponse response6 = await azureOpenAIAgent.RunAsync("Hello");
Console.WriteLine(response6);


//### 7.Microsoft Foundary
// Packages:
//  Azure.Identity
//  Microsoft.Agents.AI.AzureAI

AIProjectClient mfClient = new(new Uri(""), new AzureCliCredential());
ClientResult<AgentVersion> foundryAgent = await mfClient.Agents.CreateAgentVersionAsync(
    agentName: "MyAgent",
    options: new AgentVersionCreationOptions(new PromptAgentDefinition("<YourModel>"))
    );
ChatClientAgent agentFrameworkAgent = mfClient.GetAIAgent(foundryAgent);

AgentRunResponse response7 = await agentFrameworkAgent.RunAsync("Hello");
Console.WriteLine(response7);

// ### 8.Amazon Bedrock
// Packages:
// AWSSDK.Extentions.Bedrock.MEAI
// Microsoft.Agents.AI
Environment.SetEnvironmentVariable("AWS_BEARER_TOKEN_BEDROCK", "<YourAPIKey>");
AmazonBedrockRuntimeClient runtimeClient = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);
ChatClientAgent amzAgent = new(runtimeClient.AsIChatClient("<YourModel>"));
AgentRunResponse response8 = await amzAgent.RunAsync("Hello");
Console.WriteLine(response8);

// ### 9. Ollama (Offline Model from the machine)
// Packages:
//  OllamaSharp
//  Microsoft.Agents.AI
IChatClient ollamaApiClient = new OllamaApiClient("http://localhost:11434", "");
ChatClientAgent ollamaAgent = new ChatClientAgent(ollamaApiClient);
AgentRunResponse response9 = await ollamaAgent.RunAsync("Hello");
Console.WriteLine(response9);


// ### 10. Foundary Local (Offline local models)
// Packages:
// Microsoft.Agents.AI.OpenAI
// Microsoft.AI.Foundry.Local
/*string modelAlias = "<YourModel>";
FoundryLocalManager manager = await FoundryLocalManager.StartModelAsync(modelAlias);
ModelInfo? modelInfo = await manager.GetModelInfoAsync(modelAlias);
OpenAIClient mflocalClient = new(new(ApiKeyCredential("NO_API_KEY"), new OpenAIClientOptions
{
    Endpoint = manager.Endpoint

}));
ChatClientAgent mfLocalAgent = mflocalClient.GetChatClient(modelInfo!.ModelId).CreateAIAgent();
AgentRunResponse response10 = await mfLocalAgent.RunAsync("Hello");
Console.WriteLine(response10); */

// 11. OpenRouter (https://openrouter.ai/settings/keys
// packages :
// Microoft.Agents.AI.OpenAI
OpenAIClient openAIclient = new OpenAIClient(new ApiKeyCredential("<YourApiKey"), new OpenAIClientOptions
{
    Endpoint = new Uri("https://openrouter.api/api/v1")
});
ChatClientAgent routerAgent = openAIclient.GetChatClient("<yourModelName").CreateAIAgent();

AgentRunResponse response11 = await routerAgent.RunAsync("Hello");
Console.WriteLine(response11);

//12. Together.ai https://api.together.xyz/settings/api-keys
// packages
//  Microsoft.Agents.AI.OpenAI
OpenAIClient togetherClient = new(new ApiKeyCredential("ApiKeyHere"), new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.together.xyz/v1")
});
ChatClientAgent togetherAgent = togetherClient.GetChatClient("<YourModelName>").CreateAIAgent();
AgentRunResponse response12 = await togetherAgent.RunAsync("Hello");
Console.WriteLine(response12);


// 13. Cohere https://dashboard.cohere.com/api-keys
// packages:
//  Microsoft.Agents.AI.OpenAI
OpenAIClient cohereClient = new(new ApiKeyCredential("ApiKeyHere"), new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.cohere.ai/compatibility/v1")
});

ChatClientAgent cohereAgent = cohereClient.GetChatClient("YourModelName").CreateAIAgent();
AgentRunResponse response13 = await cohereAgent.RunAsync("Hello");
System.Console.WriteLine(response13);

// 14. HuggingFace https://huggingface.co/settings/tokens
// packages : 
//  Microsoft.Agents.AI.OpenAI
OpenAIClient hfClient = new(new ApiKeyCredential("ApiKeyHere"), new OpenAIClientOptions
{
    Endpoint = new Uri("https://router.huggingface.co/v1")
});

ChatClientAgent hfAgent = hfClient.GetChatClient("YourModelName").CreateAIAgent();
AgentRunResponse response14 = await cohereAgent.RunAsync("Hello");
System.Console.WriteLine(response14);


// 15. Github Models https://github.com/settings/personal-access-tokens
// packages:
    // Azure.AI.Inference
    // Microsoft.Agents.AI
    // Microsoft.Extensions.AI.AzureAIInference
const string githubPatToken = "YourGitHubPersonalAccessToken";
const string model = "yourmodel";

ChatClientAgent githubAgent = new ChatCompletionsClient(
    new Uri("https://models.github.ai/inference"),
    new Azure.AzureKeyCredential(githubPatToken),
    new AzureAIInferenceClientOptions()).AsIChatClient(model).CreateAIAgent();
AgentRunResponse response15 = await githubAgent.RunAsync("Hello");
Console.WriteLine(response15);




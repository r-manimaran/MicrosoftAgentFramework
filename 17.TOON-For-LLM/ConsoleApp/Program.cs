
using Azure.AI.OpenAI;
using ConsoleApp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Text.Json;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
    new System.ClientModel.ApiKeyCredential(LLMConfig.ApiKey));

string json = await File.ReadAllTextAsync("Specialist.json");
List<FamousPeople> list = JsonSerializer.Deserialize<List<FamousPeople>>(json)!;

string instructions = "You answer questions about famous people. Always use tool 'get_famous_people' to get data";
string question = "Tell me about Hula Johnson";

ChatClientAgent agentWithJsonTool = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(instructions: instructions,
    tools: [AIFunctionFactory.Create(GetFamousPeopleAsJson, name: "get_famous_people")]);

ChatClientAgent agentWithToonTool = client.GetChatClient(LLMConfig.DeploymentOrModelId)
    .CreateAIAgent(instructions: instructions,
    tools: [AIFunctionFactory.Create(GetFamousPeopleAsToon, name: "get_famous_people")]);

Utils.WriteLineInformation("Ask using Json tool");

AgentRunResponse response1 = await agentWithJsonTool.RunAsync(question);
Console.WriteLine(response1);
response1.Usage.OutputAsInformation();

Utils.WriteLineInformation("Ask using Toon tool");
AgentRunResponse response2  = await agentWithToonTool.RunAsync(question);
Console.WriteLine(response2);
response2.Usage.OutputAsInformation();


List<FamousPeople> GetFamousPeopleAsJson()
{
    string json = JsonSerializer.Serialize(list);
    return list;
}

string GetFamousPeopleAsToon()
{
    string toon = ToonNetSerializer.ToonNet.Encode(list);
    List<FamousPeople?> decodedAgain = ToonNetSerializer.ToonNet.Decode<List<FamousPeople>>(toon);
    return toon;
}


    
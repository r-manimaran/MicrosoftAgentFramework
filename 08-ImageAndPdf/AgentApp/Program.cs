using AgentApp;
using AgentApp.Extensions;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                                    new Azure.AzureKeyCredential(LLMConfig.ApiKey));

OpenAIClient openAiClient = new OpenAIClient(new ApiKeyCredential(LLMConfig.OpenAiKey));

AIAgent azureOpenAiAgent = client.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent();

// AzuureOpenAIAgent does not currently support PDF inputs, so we use OpenAIClient for that.
AIAgent openAiAgent = openAiClient.GetChatClient(LLMConfig.DeploymentOrModelId).CreateAIAgent();

Scenario scenario = Scenario.Image;
AgentRunResponse response;
switch (scenario)
{
    case Scenario.Text:
    {
            response = await azureOpenAiAgent.RunAsync("Tell me a joke about programming.");
            ShowResponse(response);
            break;
    }
    case Scenario.Image:
    {
            //Image via Uri
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is in this image?"),
            new UriContent("https://upload.wikimedia.org/wikipedia/commons/9/93/M%C3%BCnster%2C_Schlossplatz%2C_Herbstsend%2C_Kettenkarussell_--_2024_--_6459.jpg","image/jpeg")
                ]));
            ShowResponse(response);

            //LocalFile
            string path = Path.Combine( "azure-architecture-diagram.png");

            //Image via base64
            string base64Image = Convert.ToBase64String(File.ReadAllBytes(path));
            string dataUri = $"data:image/jpeg;base64,{base64Image}";
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is in this image?"),
                    new DataContent(dataUri, "image/jpeg")
                    ]));
            ShowResponse(response);
            //---------------------------------
            // Image via Memory 
            ReadOnlyMemory<byte> data = File.ReadAllBytes(path);
            response = await azureOpenAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                    new TextContent("What is in this image?"),
                    new DataContent(data, "image/jpeg")
                ]));
            ShowResponse(response);
            break;
    }
    case Scenario.Pdf:
    {
            string path = Path.Combine( "AgentFramework.pdf");
            //Pdf base64
            string base64Pdf = Convert.ToBase64String(File.ReadAllBytes(path));
            string dataUri = $"data:application/pdf;base64,{base64Pdf}";
            response = await openAiAgent.RunAsync(new ChatMessage(ChatRole.User,
                [
                    new TextContent("Summarize this document."),
                    new DataContent(dataUri, "application/pdf")
                ]));
            ShowResponse(response);
            break;
    }
}

void ShowResponse(AgentRunResponse response)
{
    Console.WriteLine("Response:");
    Console.WriteLine(response);
    Utils.WriteLineInformation("************************************");
    Utils.WriteLineInformation($"- Input Tokens:{response.Usage?.InputTokenCount}");
    Utils.WriteLineInformation($"- Output Tokens:{response.Usage?.OutputTokenCount}" +
        $"({response.Usage?.GetOutputTokensUsedForReasoning()} was used for reasoning)");
    Utils.Separator();
}
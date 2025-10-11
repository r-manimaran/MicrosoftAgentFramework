using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Reflection;
using System.Text;
using ToolCalling.Advanced;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(AIConfig.Endpoint), new Azure.AzureKeyCredential(AIConfig.ApiKey));

// Get the tools using reflection
MethodInfo[] methods = typeof(FileSystemTools).GetMethods(BindingFlags.Public | BindingFlags.Instance);

List<AITool> listOfTools = methods.Select(x=> AIFunctionFactory.Create(x,new FileSystemTools())).Cast<AITool>().ToList();

// Create the agent
AIAgent agent = client.GetChatClient(AIConfig.DeploymentOrModelId)
    .CreateAIAgent(
    instructions: "You are a File system Expert. When working with files you need to provide the full path; not just the filename",
    tools: listOfTools
    )    
    .AsBuilder()
    .Use(FunctionCallingMiddleware) // Calling the Middleware to handle function calling and user approvals
    .Build();


AgentThread thread = agent.GetNewThread();

// Chat 
while(true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input == "exit") break;

    ChatMessage userMessage = new ChatMessage(ChatRole.User, input);
    AgentRunResponse response = await agent.RunAsync(userMessage, thread);

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    List<UserInputRequestContent> userInputRequests = response.UserInputRequests.ToList();

    while(userInputRequests.Count > 0)
    {
        List<ChatMessage> userInputResponses = userInputRequests
            .OfType<FunctionApprovalRequestContent>()
            .Select(functionApprovalRequest =>
            {
                Console.WriteLine($"The agent would like to invoke the following function, please reply with Y to approve or N to reject:Name {functionApprovalRequest.FunctionCall.Name}");
                return new ChatMessage(ChatRole.User, [functionApprovalRequest.CreateResponse(Console.ReadLine()?.Equals("Y", StringComparison.OrdinalIgnoreCase)??false)]);
            }).ToList();
        response = await agent.RunAsync(userInputResponses, thread);
        userInputRequests = response.UserInputRequests.ToList();
    }
    Console.WriteLine(response);

    Utils.Separator();
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}

async ValueTask<object?> FunctionCallingMiddleware(
    AIAgent callingAgent, 
    FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken,ValueTask<object?>> next, 
    CancellationToken token)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"Tool Call:'{context.Function.Name}'");
    if(context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(", ", context.Arguments.Select(kv => $"{kv.Key}:{kv.Value}"))})");
    }  
    Utils.WriteLineInformation(functionCallDetails.ToString());
    return await next(context, token);
}
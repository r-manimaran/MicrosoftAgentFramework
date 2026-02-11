using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using OpenAI.Chat;
using QuizApp;
using System.ClientModel;

AzureOpenAIClient client = new AzureOpenAIClient(new Uri(LLMConfig.Endpoint),
                                new ApiKeyCredential(LLMConfig.ApiKey));
ChatClient chatClient = client.GetChatClient(LLMConfig.DeploymentOrModelId);

var agent = chatClient.AsAIAgent(instructions: "You are the judge in a guessing game where it is about guessing animals." +
    "Each hint should only give one fact");

List<string> animals = new List<string>() { "dog", "cat", "rabbit", "elephant", "giraffe", "shark", "Lion","tiger","Panda","Dolphin" };

var animalToGuess = animals[new Random().Next(animals.Count)];
var initialHintResponse =await agent.RunAsync<string>($"Make a vague hint for the animal: '{animalToGuess}'");

// Define the Workflow
RequestPort requestPort = RequestPort.Create<FeedbackToUser, string>("GuessAnimal");
var evaluateAndHintExecutor = new EvaluateAndHintExecutor(agent, animalToGuess);
var workflow = new WorkflowBuilder(requestPort)
    .AddEdge(requestPort, evaluateAndHintExecutor)
    .AddEdge(evaluateAndHintExecutor, requestPort)
    .WithOutputFrom(evaluateAndHintExecutor)
    .Build();

var initialFeedback = new FeedbackToUser(initialHintResponse.Result, Init: true);
await using StreamingRun handle = await InProcessExecution.StreamAsync(workflow, initialFeedback);

await foreach(WorkflowEvent evt in handle.WatchStreamAsync())
{
    switch (evt)
    {
        case RequestInfoEvent requestInputEvt:
            var externalRequest = requestInputEvt.Request;
            if (externalRequest.DataIs<FeedbackToUser>())
            {
                FeedbackToUser feedbackToUser = externalRequest.DataAs<FeedbackToUser>()!;
                Utils.WriteLineInformation(feedbackToUser.Init ?
                    "Guess what animal I'm thinking of!" :
                    "That is not the animal I'm thinking of");
                Console.WriteLine($"Hint: {feedbackToUser.Hint}");
                Utils.Separator();
                                Console.Write("Your guess: ");
                string userGuess = Console.ReadLine() ?? string.Empty;
                if(string.IsNullOrEmpty(userGuess))
                {
                    Utils.WriteLineWarning("Empty guess, please try again.");
                    userGuess = Console.ReadLine()!;
                }
                var externalResponse = externalRequest.CreateResponse(userGuess);
                await handle.SendResponseAsync(externalResponse);
                break;
            }
            throw new NotSupportedException($"Request {externalRequest.PortInfo.RequestType} is not supported."); 
        case WorkflowOutputEvent outputEvt:
            Utils.WriteLineSuccess(outputEvt.Data.ToString()!);
            return;
    }
}


class EvaluateAndHintExecutor(ChatClientAgent agent, string animalToGuess): Executor<string>("Evaluator and Hint-giver")
{
    private int _numberOfTries;
    private readonly IList<string> _hintsGiven = [];

    public override async ValueTask HandleAsync(string message, 
        IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _numberOfTries++;
        var input = $"Is this the right answer for guessing the animal -'{animalToGuess}'? (allow for spelling errors of the animal, but not the different animal): {message}";
        ChatClientAgentResponse<bool> isRightAnswerResponse = await agent.RunAsync<bool>(input, cancellationToken: cancellationToken);
        if (isRightAnswerResponse.Result)
        {
            // The guess is correct, end the game
            await context.YieldOutputAsync("Congratulations! You guessed it." +
                $"The answer is indeed a '{animalToGuess}' and it took you {_numberOfTries} tries.", cancellationToken);
        }
        else
        {
            // Not correct answer, give another hint
            var newHintPrompt = $"Generate a hint for a child to guess the animal.'{animalToGuess}'." +
                $"Hint already given: {string.Join(" | ", _hintsGiven)}." +
                $"so make the new hit unique and d not repeat the same hint parts.";
            ChatClientAgentResponse<string> hitResponse = await agent.RunAsync<string>(newHintPrompt, cancellationToken: cancellationToken);
            var newHint = hitResponse.Result;
            _hintsGiven.Add(newHint);
            await context.SendMessageAsync(new FeedbackToUser(newHint), cancellationToken);
        }
    }
}
record FeedbackToUser(string Hint, bool Init=false);

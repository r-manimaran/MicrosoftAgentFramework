using Azure.AI.OpenAI;
using ClassbasedSkills;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using Shared;

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Option 1: We can use directly like below
// var skill = new UnitConverterSkill();

// Option 2: We can use the DI like below
ServiceCollection servicesCollection = new();
servicesCollection.AddSingleton<UnitConverterSkill>();
IServiceProvider sp = servicesCollection.BuildServiceProvider();

// For Option 1: use like below
// var skillProvider = new AgentSkillsProvider(skill);

// For option 2: Use DI and get the Class Skill
var unitConverterSkill = sp.GetRequiredService<UnitConverterSkill>();
var skillProvider = new AgentSkillsProvider(unitConverterSkill);

AzureOpenAIClient client = Utils.GetAzureOpenAIClient(showRawCall: true);

AIAgent agent = client.GetChatClient("gpt-4o-mini").AsAIAgent(
    new ChatClientAgentOptions
    {
        Description = "Deals with unit converter",
        Name = "unit-converter-agent",
        ChatOptions = new ChatOptions()
        {
            Instructions = "Your tasks is to answer queries about the unit conversion"
        },
        AIContextProviders = [skillProvider],

    }).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();

AgentResponse response = await agent.RunAsync("Can you convert 5 miles to kilometers?");
Console.WriteLine(response);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

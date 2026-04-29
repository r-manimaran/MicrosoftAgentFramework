using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;
using System.Text.Json;

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var unitConverterSkill = new AgentInlineSkill(
    name: "unit-converter",
    description: "Convert between common units using a conversion factor",
    instructions: """
    Use this skill when the user asks to convert between units.
    1. Review the conversion-table resource to find the correct factor.
    2. Use the convert script, passing the value and factor from the table.
    3. Present the result clearly with both units.
    """)
    .AddResource(
    "conversion-table",
    """
    # Conversion Tables
    Formula: **result = value × factor**
    | From       | To         | Factor   |
    |------------|------------|----------|
    | miles      | kilometers | 1.60934  |
    | kilometers | miles      | 0.621371 |
    | pounds     | kilograms  | 0.453592 |
    | kilograms  | pounds     | 2.20462  |
    """)
    .AddScript("convert", (double value, double factor) =>
    {
        double result = Math.Round(value * factor, 4);
        return JsonSerializer.Serialize(new { value, factor, result });
    });
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

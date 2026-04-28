using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var codeStyleSkill = new AgentInlineSkill(
    name: "code-style",
    description: "Coding style guidelines and conventions for the team",
    instructions: """
    Use this skill when answering questions about coding style, conventions or best practices for the team.
    1. Read the style-guide resource for the full set of rules.
    2. Answer based on the rules, quoting the relevant guideline where helpful.
    """)
    .AddResource(
    "style-guide",
    """
    # C# & .NET Team Coding Style Guide

    ## Formatting & Layout
    - Use 4-space indentation (no tabs)
    - Maximum line length: 65 characters (120 acceptable for team projects)
    - Use "Allman" brace style: opening and closing braces each on their own line, aligned with current indentation level
    - One statement per line
    - One declaration per line
    - Add at least one blank line between method and property definitions
    - Use parentheses to make expression clauses explicit: `if ((x > y) && (x > z))`
    - Line breaks should occur before binary operators when necessary

    ## Naming Conventions
    - PascalCase: type names, namespaces, all public members, constants
    - camelCase: method arguments, local variables, private fields
    - Prefix private instance fields with underscore: `_myField`
    - Prefix private/internal static fields with `s_`: `s_workerQueue`
    - Prefix thread-static fields with `t_`: `t_timeSpan`
    - Interface names start with capital `I`: `IWorkerQueue`
    - Attribute types end with `Attribute`: `SerializableAttribute`
    - Enum types use singular noun (non-flags) or plural noun (flags)
    - Generic type parameters use descriptive PascalCase names prefixed with `T`: `TEntity`
    - Primary constructor parameters on `class`/`struct`: camelCase
    - Primary constructor parameters on `record`: PascalCase (they become public properties)
    - Prefer clarity over brevity; avoid abbreviations unless widely known
    - No single-letter names except simple loop counters (`i`, `j`, `k`)

    ## Comments
    - Use single-line comments `//` for brief explanations
    - Avoid block comments `/* */`
    - Use XML doc comments (`///`) for all public types, methods, fields, and properties
    - Place comments on their own line, not at end of a code line
    - Begin comment text with an uppercase letter and end with a period
    - Insert one space between `//` and comment text: `// This is a comment.`

    ## Type Usage
    - Use language keywords over framework type names: `string` not `String`, `int` not `Int32`
    - Use `int` over unsigned types unless the domain specifically requires unsigned
    - Use `var` only when the type is obvious from the right-hand side of the assignment
    - Do not use `var` when type isn't immediately clear from the expression
    - Do not use `var` in place of `dynamic`
    - Use `var` for loop variables in `for` loops
    - Use explicit types in `foreach` loops

    ## Strings
    - Use string interpolation for short concatenation: `$"{first}, {last}"`
    - Use `StringBuilder` for string building inside loops
    - Prefer raw string literals over escape sequences or verbatim strings

    ## Object & Collection Initialization
    - Use concise `new` forms: `var x = new Foo();` or `Foo x = new();`
    - Use object initializers instead of setting properties one-by-one after construction
    - Use collection expressions to initialize all collection types: `string[] x = [ "a", "b" ];`
    - Use `required` properties instead of constructors to force property initialization

    ## Delegates & Lambdas
    - Use `Func<>` and `Action<>` instead of defining custom delegate types
    - Use lambda expressions for event handlers that don't need to be removed later

    ## Exception Handling
    - Use `try-catch` for most exception handling
    - Catch only specific, handleable exception types; avoid catching `System.Exception` without a filter
    - Use the braceless `using` syntax for disposables: `using Font f = new Font(...);`

    ## Operators
    - Use `&&` over `&` and `||` over `|` for boolean comparisons (short-circuit evaluation)

    ## Static Members
    - Call static members via the class name: `ClassName.StaticMember`
    - Never qualify a base class static member using a derived class name

    ## LINQ
    - Use meaningful names for query variables: `seattleCustomers` not `query`
    - Use Pascal casing for anonymous type property names
    - Use implicit typing (`var`) for query and range variables
    - Align query clauses under the `from` clause
    - Place `where` clauses before other query clauses
    - Use multiple `from` clauses to access inner collections instead of `join`

    ## Namespaces & Usings
    - Use file-scoped namespace declarations: `namespace MyApp.Core;`
    - Place `using` directives outside the namespace declaration

    ## Async
    - Use `async`/`await` for all I/O-bound operations
    - Use `Task.ConfigureAwait` appropriately to avoid deadlocks

    ## Security
    - Follow .NET Secure Coding Guidelines for all security-sensitive code
    """);
var skillProvider = new AgentSkillsProvider(codeStyleSkill);

AzureOpenAIClient client = Utils.GetAzureOpenAIClient(showRawCall: true);

AIAgent agent = client.GetChatClient("gpt-4o-mini").AsAIAgent(
    new ChatClientAgentOptions
{
    Description = "Deals with Coding Best practices",
    Name ="Code-Best-Practice-Agent",
    ChatOptions = new ChatOptions()
    {
        Instructions = "Your tasks is to answer developer queries about the coding best practices"
    },
    AIContextProviders = [skillProvider],
    
}).AsBuilder().Use(Utils.ToolCallingMiddleware).Build();

AgentResponse response = await agent.RunAsync("Can I use tabs in my C# code beginning of the line?");
Console.WriteLine(response);




#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


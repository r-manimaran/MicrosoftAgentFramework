var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AgentAppWebApi>("agentappwebapi");

builder.Build().Run();

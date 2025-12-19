var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ChatApp>("chatapp");

builder.AddProject<Projects.WebApiChat>("webapichat");

builder.Build().Run();

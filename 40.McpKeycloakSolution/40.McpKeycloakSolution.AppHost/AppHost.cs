var builder = DistributedApplication.CreateBuilder(args);

// Keycloak container - import realm on startup
var keycloak = builder.AddKeycloak("keycloak",8081)
                    .WithDataVolume()
                    .WithRealmImport("./keycloak/realm-export.json");

var mcpServer = builder.AddProject<Projects.McpServer>("mcpserver")
                        .WithReference(keycloak)
                        .WaitFor(keycloak);
                        //.WithHttpEndpoint(port: 5100, name: "http");

builder.Build().Run();

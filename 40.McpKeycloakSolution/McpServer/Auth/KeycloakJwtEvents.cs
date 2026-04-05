using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text.Json;

namespace McpServer.Auth;

public class KeycloakJwtEvents : JwtBearerEvents
{
    public override Task TokenValidated(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        // realm_access.roles --> standard role claims
        var realmAccess = context.Principal.FindFirstValue("realm_access");

        if (realmAccess is not null)
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if(doc.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach(var role in roles.EnumerateArray())
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                }
            }
        }
        return Task.CompletedTask;
    }
}

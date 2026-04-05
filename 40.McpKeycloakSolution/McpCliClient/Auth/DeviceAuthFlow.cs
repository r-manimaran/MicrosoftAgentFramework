using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace McpCliClient.Auth;

public class DeviceAuthFlow
{
    private readonly HttpClient _http;
    private readonly string _keycloakBase;
    private const string Realm = "mcp-realm";
    private const string ClientId = "mcp-cli";

    public DeviceAuthFlow(HttpClient http, string keycloakBase)
    {
        _http = http;
        _keycloakBase = keycloakBase.TrimEnd('/');
    }

    public async Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        var deviceEndpoint = $"{_keycloakBase}/realms/{Realm}/protocol/openid-connect/auth/device";
        var tokenEndpoint = $"{_keycloakBase}/realms/{Realm}/protocol/openid-connect/token";

        // Step 1 - request device & user codes
        var deviceResp = await _http.PostAsync(deviceEndpoint,
                        new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            ["client_id"] = ClientId,
                            ["scope"] = "openid profile email"
                        }), cancellationToken);
        deviceResp.EnsureSuccessStatusCode();
        var deviceJson = JsonDocument.Parse(await deviceResp.Content.ReadAsStringAsync());
        var root = deviceJson.RootElement;

        var deviceCode = root.GetProperty("device_code").GetString()!;
        var userCode = root.GetProperty("user_code").GetString()!;
        var verifyUri = root.GetProperty("verification_uri_complete").GetString()!;
        var interval = root.TryGetProperty("interval", out var iv) ? iv.GetInt32() : 5;
        var expiresIn = root.GetProperty("expires_in").GetInt32();

        // Step 2 - instruct the user.
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n Open this URL in your browser to authenticate:");
        Console.WriteLine($"\n {verifyUri}");
        Console.WriteLine($"\n User Code : {userCode}");
        Console.ResetColor();
        Console.WriteLine($"\n Waiting for authentication (expires in {expiresIn}s");

        // Step 3 - Poll the token endpoint
        var deadline = DateTime.UtcNow.AddSeconds(expiresIn);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);

            var tokenResp = await _http.PostAsync(tokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["device_code"] = deviceCode
                }), cancellationToken);

            var tokenJson = JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync(cancellationToken));

            if (tokenResp.IsSuccessStatusCode)
            {
                var token = tokenJson.RootElement.GetProperty("access_token").GetString()!;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  Authentication successful!\n");
                Console.ResetColor();
                return token;
            }

            var error = tokenJson.RootElement
                .TryGetProperty("error", out var e) ? e.GetString() : "unknown";

            if (error == "authorization_pending") continue;          // keep polling
            if (error == "slow_down") { interval += 5; continue; }   // back off
            throw new Exception($"Device auth failed: {error}");
        }

        throw new TimeoutException("Device authorization timed out.");
    }
}

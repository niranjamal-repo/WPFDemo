using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebApi.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "wpfdemo-test",
                ["Jwt:Audience"] = "wpfdemo-test",
                ["Jwt:Key"] = "test-signing-key-1234567890-abcdefghijklmnopqrstuvwxyz",
                ["AzureAppConfiguration:ConnectionString"] = string.Empty,
                ["Azure:KeyVaultUri"] = string.Empty,
                ["Users:0:UserName"] = "admin",
                ["Users:0:Password"] = "Admin@123",
                ["Users:0:Role"] = "Admin",
                ["Users:1:UserName"] = "user",
                ["Users:1:Password"] = "User@123",
                ["Users:1:Role"] = "User"
            };

            config.AddInMemoryCollection(overrides);
        });
    }
}

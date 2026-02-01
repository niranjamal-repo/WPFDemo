using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared;
using WebApi.Models;

namespace WebApi.Tests;

public class ItemsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ItemsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Items_Returns_Ok()
    {
        var response = await _client.GetAsync("/api/v1/items?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.True(response.Headers.Contains("Page"));
        Assert.True(response.Headers.Contains("PageSize"));
        var items = await response.Content.ReadFromJsonAsync<List<Item>>();
        Assert.NotNull(items);
    }

    [Fact]
    public async Task Create_Item_Then_Get_By_Id()
    {
        var token = await GetTokenAsync("admin-user", "Admin");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/items", new Item
        {
            Name = "Integration Test Item",
            Description = "Created by integration test",
            Price = 42.5m
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Item>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/v1/items/{created!.Id}");

        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<Item>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
    }

    private async Task<string> GetTokenAsync(string userName, string role)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/token", new
        {
            userName,
            role
        });

        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse?.AccessToken ?? string.Empty;
    }
}

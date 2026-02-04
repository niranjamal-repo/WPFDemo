using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared;
using WpfClient.Models;

namespace WpfClient.Services;

public class ItemApiClient
{
    private readonly HttpClient _httpClient;

    public ItemApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<ItemPageResult> GetItemsAsync(
        string? search,
        string? sortBy,
        string? sortDir,
        int page,
        int pageSize)
    {
        var url =
            $"api/v1/items?search={Uri.EscapeDataString(search ?? string.Empty)}&sortBy={sortBy}&sortDir={sortDir}&page={page}&pageSize={pageSize}";
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<Item>>() ?? new List<Item>();
        var totalCount = TryGetHeaderInt(response, "X-Total-Count");
        var pageHeader = TryGetHeaderInt(response, "Page");
        var pageSizeHeader = TryGetHeaderInt(response, "PageSize");

        return new ItemPageResult(
            items,
            totalCount ?? items.Count,
            pageHeader ?? page,
            pageSizeHeader ?? pageSize);
    }

    public async Task<Item> CreateAsync(Item item)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/items", item);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Item>())!;
    }

    public async Task UpdateAsync(int id, Item item)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/items/{id}", item);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/items/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<TokenResponse> RequestTokenAsync(TokenRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/token", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    public void SetAccessToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static int? TryGetHeaderInt(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

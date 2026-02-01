using Shared;

namespace WpfClient.Models;

public record ItemPageResult(IReadOnlyList<Item> Items, int TotalCount, int Page, int PageSize);

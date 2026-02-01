using Shared;
using WebApi.Repositories;

namespace WebApi.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task GetAllAsync_Filters_By_Search_Text()
    {
        var repo = new InMemoryItemRepository();
        await repo.CreateAsync(new Item { Name = "Desk Lamp", Description = "LED light", Price = 35 });

        var results = await repo.GetPagedAsync("lamp", "name", "asc", null, null, null, null, 1, 50);

        Assert.Contains(results.Items, item => item.Name.Contains("Lamp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_Sorts_By_Price()
    {
        var repo = new InMemoryItemRepository();
        await repo.CreateAsync(new Item { Name = "Budget", Description = "Cheap", Price = 10 });
        await repo.CreateAsync(new Item { Name = "Premium", Description = "Expensive", Price = 200 });

        var results = await repo.GetPagedAsync(null, "price", "desc", null, null, null, null, 1, 50);

        Assert.True(results.Items.Count >= 2);
        Assert.True(results.Items[0].Price >= results.Items[1].Price);
    }

    [Fact]
    public async Task UpdateAsync_Returns_False_When_Not_Found()
    {
        var repo = new InMemoryItemRepository();

        var result = await repo.UpdateAsync(999, new Item { Name = "Missing", Description = "None", Price = 0 });

        Assert.False(result);
    }
}

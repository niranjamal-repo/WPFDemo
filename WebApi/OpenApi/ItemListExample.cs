using Shared;
using Swashbuckle.AspNetCore.Filters;

namespace WebApi.OpenApi;

public class ItemListExample : IExamplesProvider<List<Item>>
{
    public List<Item> GetExamples()
    {
        return new List<Item>
        {
            new()
            {
                Id = 1,
                Name = "Laptop",
                Description = "14-inch ultrabook",
                Price = 1200m,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = 2,
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = 25m,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };
    }
}

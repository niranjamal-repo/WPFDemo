using Shared;
using Swashbuckle.AspNetCore.Filters;

namespace WebApi.OpenApi;

public class ItemExample : IExamplesProvider<Item>
{
    public Item GetExamples()
    {
        return new Item
        {
            Id = 1,
            Name = "Laptop",
            Description = "14-inch ultrabook",
            Price = 1200m,
            CreatedAt = DateTime.UtcNow
        };
    }
}

using Shared;
using WebApi.Models;

namespace WebApi.Repositories;

public class InMemoryItemRepository : IItemRepository
{
    private readonly List<Item> _items = new();
    private int _nextId = 1;
    private readonly object _sync = new();

    public InMemoryItemRepository()
    {
        AddSeed("Laptop", "14-inch ultrabook", 1200m);
        AddSeed("Mouse", "Wireless mouse", 25m);
        AddSeed("Keyboard", "Mechanical keyboard", 95m);
    }

    public async Task<PagedResult<Item>> GetPagedAsync(
        string? search,
        string? sortBy,
        string? sortDir,
        decimal? minPrice,
        decimal? maxPrice,
        DateTime? fromCreated,
        DateTime? toCreated,
        int page,
        int pageSize)
    {
        await Task.Yield();

        IEnumerable<Item> query;
        lock (_sync)
        {
            query = _items
                .Select(item => new Item
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    CreatedAt = item.CreatedAt
                })
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(item => item.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(item => item.Price <= maxPrice.Value);
        }

        if (fromCreated.HasValue)
        {
            query = query.Where(item => item.CreatedAt >= fromCreated.Value);
        }

        if (toCreated.HasValue)
        {
            query = query.Where(item => item.CreatedAt <= toCreated.Value);
        }

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy ?? "createdAt").ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(item => item.Name) : query.OrderBy(item => item.Name),
            "price" => descending ? query.OrderByDescending(item => item.Price) : query.OrderBy(item => item.Price),
            "createdat" => descending ? query.OrderByDescending(item => item.CreatedAt) : query.OrderBy(item => item.CreatedAt),
            _ => descending ? query.OrderByDescending(item => item.CreatedAt) : query.OrderBy(item => item.CreatedAt)
        };

        var totalCount = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Item>(items, totalCount, page, pageSize);
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        await Task.Yield();
        lock (_sync)
        {
            return _items.FirstOrDefault(item => item.Id == id);
        }
    }

    public async Task<Item> CreateAsync(Item item)
    {
        await Task.Yield();
        lock (_sync)
        {
            var created = new Item
            {
                Id = _nextId++,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                CreatedAt = DateTime.UtcNow
            };
            _items.Add(created);
            return created;
        }
    }

    public async Task<bool> UpdateAsync(int id, Item item)
    {
        await Task.Yield();
        lock (_sync)
        {
            var existing = _items.FirstOrDefault(i => i.Id == id);
            if (existing is null)
            {
                return false;
            }

            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.Price = item.Price;
            return true;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await Task.Yield();
        lock (_sync)
        {
            var existing = _items.FirstOrDefault(i => i.Id == id);
            if (existing is null)
            {
                return false;
            }

            _items.Remove(existing);
            return true;
        }
    }

    private void AddSeed(string name, string description, decimal price)
    {
        _items.Add(new Item
        {
            Id = _nextId++,
            Name = name,
            Description = description,
            Price = price,
            CreatedAt = DateTime.UtcNow
        });
    }
}

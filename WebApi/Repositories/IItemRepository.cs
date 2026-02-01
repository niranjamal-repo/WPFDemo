using Shared;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IItemRepository
{
    Task<PagedResult<Item>> GetPagedAsync(
        string? search,
        string? sortBy,
        string? sortDir,
        decimal? minPrice,
        decimal? maxPrice,
        DateTime? fromCreated,
        DateTime? toCreated,
        int page,
        int pageSize);
    Task<Item?> GetByIdAsync(int id);
    Task<Item> CreateAsync(Item item);
    Task<bool> UpdateAsync(int id, Item item);
    Task<bool> DeleteAsync(int id);
}

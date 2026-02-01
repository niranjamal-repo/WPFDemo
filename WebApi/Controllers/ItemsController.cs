using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Shared;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using WebApi.OpenApi;
using WebApi.Repositories;

namespace WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IItemRepository _repository;
    private readonly IFeatureManager _featureManager;

    public ItemsController(IItemRepository repository, IFeatureManager featureManager)
    {
        _repository = repository;
        _featureManager = featureManager;
    }

    [HttpGet]
    [AllowAnonymous]
    [SwaggerResponse(200, "List of items", typeof(IEnumerable<Item>))]
    [SwaggerResponseExample(200, typeof(ItemListExample))]
    public async Task<ActionResult<IReadOnlyList<Item>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateTime? fromCreated,
        [FromQuery] DateTime? toCreated,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _repository.GetPagedAsync(
            search,
            sortBy,
            sortDir,
            minPrice,
            maxPrice,
            fromCreated,
            toCreated,
            page,
            pageSize);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["Page"] = result.Page.ToString();
        Response.Headers["PageSize"] = result.PageSize.ToString();

        return Ok(result.Items);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [SwaggerResponse(200, "Single item", typeof(Item))]
    [SwaggerResponseExample(200, typeof(ItemExample))]
    public async Task<ActionResult<Item>> GetById(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [SwaggerRequestExample(typeof(Item), typeof(ItemExample))]
    [SwaggerResponse(201, "Created item", typeof(Item))]
    [SwaggerResponseExample(201, typeof(ItemExample))]
    public async Task<ActionResult<Item>> Create(Item item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            return BadRequest("Name is required.");
        }

        var created = await _repository.CreateAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, Item item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            return BadRequest("Name is required.");
        }

        var updated = await _repository.UpdateAsync(id, item);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _featureManager.IsEnabledAsync("EnableDelete"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Delete is disabled by feature flag.");
        }

        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

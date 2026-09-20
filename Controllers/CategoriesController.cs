using Microsoft.AspNetCore.Mvc;
using cs_api_v1.Dtos;
using cs_api_v1.Services;

namespace cs_api_v1.Controllers;

// Tipis ala Spring @RestController: binding + status code saja,
// aturan bisnis di ICategoryService, error mapping di ApiExceptionHandler.
[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List([FromQuery] string? search) =>
        Ok(await categories.ListAsync(search));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<CategoryResponse>> Get(Guid uuid) =>
        Ok(await categories.GetAsync(uuid));

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest req)
    {
        var created = await categories.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { uuid = created.Uuid }, created);
    }

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid uuid, UpdateCategoryRequest req) =>
        Ok(await categories.UpdateAsync(uuid, req));

    [HttpDelete("{uuid:guid}")]
    public async Task<IActionResult> Delete(Guid uuid)
    {
        await categories.DeleteAsync(uuid);
        return NoContent();
    }
}

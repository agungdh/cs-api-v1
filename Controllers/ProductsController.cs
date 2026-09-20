using Microsoft.AspNetCore.Mvc;
using cs_api_v1.Dtos;
using cs_api_v1.Services;

namespace cs_api_v1.Controllers;

// Tipis ala Spring @RestController: binding + status code saja,
// aturan bisnis di IProductService, error mapping di ApiExceptionHandler.
[ApiController]
[Route("api/products")]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CursorResponse<ProductResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryUuid,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 10) =>
        Ok(await products.ListAsync(search, categoryUuid, cursor, limit));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid uuid) =>
        Ok(await products.GetAsync(uuid));

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest req)
    {
        var created = await products.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { uuid = created.Uuid }, created);
    }

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid uuid, UpdateProductRequest req) =>
        Ok(await products.UpdateAsync(uuid, req));

    [HttpDelete("{uuid:guid}")]
    public async Task<IActionResult> Delete(Guid uuid)
    {
        await products.DeleteAsync(uuid);
        return NoContent();
    }
}

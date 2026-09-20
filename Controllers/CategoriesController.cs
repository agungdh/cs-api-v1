using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;

namespace cs_api_v1.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List([FromQuery] string? search)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Uuid, c.Name, c.Description, c.CreatedAt, c.UpdatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<CategoryResponse>> Get(Guid uuid)
    {
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Uuid == uuid);
        return category is null
            ? NotFound()
            : Ok(ToResponse(category));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest req)
    {
        var errors = Validate(req.Name);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        if (await db.Categories.AnyAsync(c => c.Name == req.Name.Trim()))
            return Conflict($"Category '{req.Name.Trim()}' sudah ada.");

        var now = DateTime.UtcNow;
        var category = new Category
        {
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { uuid = category.Uuid }, ToResponse(category));
    }

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid uuid, UpdateCategoryRequest req)
    {
        var errors = Validate(req.Name);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Uuid == uuid);
        if (category is null)
            return NotFound();

        category.Name = req.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }

    [HttpDelete("{uuid:guid}")]
    public async Task<IActionResult> Delete(Guid uuid)
    {
        var category = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Uuid == uuid);
        if (category is null)
            return NotFound();
        if (category.Products.Count > 0)
            return Conflict("Category masih dipakai product, tidak bisa dihapus.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryResponse ToResponse(Category c) =>
        new(c.Uuid, c.Name, c.Description, c.CreatedAt, c.UpdatedAt);

    private static Dictionary<string, string[]> Validate(string name)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            errors["name"] = ["Name wajib diisi, maksimal 200 karakter."];
        return errors;
    }
}

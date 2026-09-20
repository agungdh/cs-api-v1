using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;

namespace cs_api_v1.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryUuid,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        if (categoryUuid is not null)
            query = query.Where(p => p.Category != null && p.Category.Uuid == categoryUuid);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Uuid, p.Name, p.Description, p.Price, p.Stock,
                p.Category != null ? p.Category.Uuid : null,
                p.Category != null ? p.Category.Name : null,
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync();

        return Ok(new PagedResponse<ProductResponse>(items, total, page, pageSize));
    }

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid uuid)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Category).FirstOrDefaultAsync(p => p.Uuid == uuid);
        return product is null
            ? NotFound()
            : Ok(ToResponse(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest req)
    {
        var errors = Validate(req.Name, req.Price, req.Stock);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        Category? category = null;
        if (req.CategoryUuid is not null)
        {
                category = await db.Categories.FirstOrDefaultAsync(c => c.Uuid == req.CategoryUuid);
                if (category is null)
                    return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                    {
                        ["categoryUuid"] = ["Category tidak ditemukan."]
                    }));
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Price = req.Price,
            Stock = req.Stock,
            CategoryId = category != null ? category.Id : null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        product.Category = category;
        return CreatedAtAction(nameof(Get), new { uuid = product.Uuid }, ToResponse(product));
    }

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid uuid, UpdateProductRequest req)
    {
        var errors = Validate(req.Name, req.Price, req.Stock);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors));

        var product = await db.Products.FirstOrDefaultAsync(p => p.Uuid == uuid);
        if (product is null)
            return NotFound();

        int? categoryId = null;
        if (req.CategoryUuid is not null)
        {
                var category = await db.Categories.FirstOrDefaultAsync(c => c.Uuid == req.CategoryUuid);
                if (category is null)
                    return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                    {
                        ["categoryUuid"] = ["Category tidak ditemukan."]
                    }));
            categoryId = category.Id;
        }

        product.Name = req.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        product.Price = req.Price;
        product.Stock = req.Stock;
        product.CategoryId = categoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await db.Entry(product).Reference(p => p.Category).LoadAsync();
        return Ok(ToResponse(product));
    }

    [HttpDelete("{uuid:guid}")]
    public async Task<IActionResult> Delete(Guid uuid)
    {
        var deleted = await db.Products.Where(p => p.Uuid == uuid).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Uuid, p.Name, p.Description, p.Price, p.Stock,
            p.Category?.Uuid, p.Category?.Name, p.CreatedAt, p.UpdatedAt);

    private static Dictionary<string, string[]> Validate(string name, decimal price, int stock)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            errors["name"] = ["Name wajib diisi, maksimal 200 karakter."];
        if (price < 0)
            errors["price"] = ["Price tidak boleh negatif."];
        if (stock < 0)
            errors["stock"] = ["Stock tidak boleh negatif."];
        return errors;
    }
}

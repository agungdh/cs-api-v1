using Microsoft.EntityFrameworkCore;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;

namespace cs_api_v1.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", async (
            AppDbContext db,
            string? search,
            int page = 1,
            int pageSize = 10) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = db.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductResponse(
                    p.Id, p.Name, p.Description, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt))
                .ToListAsync();

            return Results.Ok(new PagedResponse<ProductResponse>(items, total, page, pageSize));
        }).WithName("ListProducts");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return product is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(product));
        }).WithName("GetProduct");

        group.MapPost("/", async (CreateProductRequest req, AppDbContext db) =>
        {
            var errors = Validate(req.Name, req.Price, req.Stock);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = req.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                Price = req.Price,
                Stock = req.Stock,
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created($"/api/products/{product.Id}", ToResponse(product));
        }).WithName("CreateProduct");

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest req, AppDbContext db) =>
        {
            var errors = Validate(req.Name, req.Price, req.Stock);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
                return Results.NotFound();

            product.Name = req.Name.Trim();
            product.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            product.Price = req.Price;
            product.Stock = req.Stock;
            product.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(product));
        }).WithName("UpdateProduct");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        }).WithName("DeleteProduct");

        return app;
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);

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

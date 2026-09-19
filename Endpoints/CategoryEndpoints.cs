using Microsoft.EntityFrameworkCore;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;

namespace cs_api_v1.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", async (AppDbContext db, string? search) =>
        {
            var query = db.Categories.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));

            var items = await query
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Uuid, c.Name, c.Description, c.CreatedAt, c.UpdatedAt))
                .ToListAsync();

            return Results.Ok(items);
        }).WithName("ListCategories");

        group.MapGet("/{uuid:guid}", async (Guid uuid, AppDbContext db) =>
        {
            var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Uuid == uuid);
            return category is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(category));
        }).WithName("GetCategory");

        group.MapPost("/", async (CreateCategoryRequest req, AppDbContext db) =>
        {
            var errors = Validate(req.Name);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            if (await db.Categories.AnyAsync(c => c.Name == req.Name.Trim()))
                return Results.Conflict($"Category '{req.Name.Trim()}' sudah ada.");

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

            return Results.Created($"/api/categories/{category.Uuid}", ToResponse(category));
        }).WithName("CreateCategory");

        group.MapPut("/{uuid:guid}", async (Guid uuid, UpdateCategoryRequest req, AppDbContext db) =>
        {
            var errors = Validate(req.Name);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var category = await db.Categories.FirstOrDefaultAsync(c => c.Uuid == uuid);
            if (category is null)
                return Results.NotFound();

            category.Name = req.Name.Trim();
            category.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            category.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(category));
        }).WithName("UpdateCategory");

        group.MapDelete("/{uuid:guid}", async (Guid uuid, AppDbContext db) =>
        {
            var category = await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Uuid == uuid);
            if (category is null)
                return Results.NotFound();
            if (category.Products.Count > 0)
                return Results.Conflict("Category masih dipakai product, tidak bisa dihapus.");

            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteCategory");

        return app;
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

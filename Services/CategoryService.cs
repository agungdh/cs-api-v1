using Microsoft.EntityFrameworkCore;
using cs_api_v1.Common.Exceptions;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;
using cs_api_v1.Services.Common;

namespace cs_api_v1.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(string? search);
    Task<CategoryResponse> GetAsync(Guid uuid);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest req);
    Task<CategoryResponse> UpdateAsync(Guid uuid, UpdateCategoryRequest req);
    Task DeleteAsync(Guid uuid);
}

// Contoh template: extend CrudService, isi mapping + validasi + Create/Update.
// Get warisan base, Delete pakai guard EnsureDeletableAsync.
public class CategoryService(AppDbContext db) : CrudService<Category>(db), ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(string? search)
    {
        var query = Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Uuid, c.Name, c.Description, c.CreatedAt, c.UpdatedAt))
            .ToListAsync();
    }

    public async Task<CategoryResponse> GetAsync(Guid uuid) =>
        ToResponse(await GetEntityAsync(uuid));

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest req)
    {
        ThrowIfInvalid(Validate(req.Name));

        if (await Db.Categories.AnyAsync(c => c.Name == req.Name.Trim()))
            throw new ConflictException($"Category '{req.Name.Trim()}' sudah ada.");

        var now = DateTime.UtcNow;
        var category = new Category
        {
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        Db.Categories.Add(category);
        await Db.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid uuid, UpdateCategoryRequest req)
    {
        ThrowIfInvalid(Validate(req.Name));

        var category = await Db.Categories.FirstOrDefaultAsync(c => c.Uuid == uuid);
        if (category is null)
            throw new NotFoundException($"Category '{uuid}' tidak ditemukan.");

        category.Name = req.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await Db.SaveChangesAsync();
        return ToResponse(category);
    }

    // Guard delete: tolak kalau masih dipakai product.
    protected override async Task EnsureDeletableAsync(Category entity)
    {
        var used = await Db.Products.AnyAsync(p => p.CategoryId == entity.Id);
        if (used)
            throw new ConflictException("Category masih dipakai product, tidak bisa dihapus.");
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

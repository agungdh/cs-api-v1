using Microsoft.EntityFrameworkCore;
using cs_api_v1.Common.Exceptions;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;
using cs_api_v1.Services.Common;

namespace cs_api_v1.Services;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> ListAsync(string? search, Guid? categoryUuid, int page, int pageSize);
    Task<ProductResponse> GetAsync(Guid uuid);
    Task<ProductResponse> CreateAsync(CreateProductRequest req);
    Task<ProductResponse> UpdateAsync(Guid uuid, UpdateProductRequest req);
    Task DeleteAsync(Guid uuid);
}

// Contoh template: extend CrudService, isi mapping + validasi + Create/Update.
// Get/Delete/pagination warisan dari base.
public class ProductService(AppDbContext db) : CrudService<Product>(db), IProductService
{
    protected override IQueryable<Product> Query() =>
        Db.Products.AsNoTracking().Include(p => p.Category);

    public async Task<PagedResponse<ProductResponse>> ListAsync(string? search, Guid? categoryUuid, int page, int pageSize)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);

        var query = Query().AsQueryable();
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

        return ToPaged(items, total, page, pageSize);
    }

    public async Task<ProductResponse> GetAsync(Guid uuid) =>
        ToResponse(await GetEntityAsync(uuid));

    public async Task<ProductResponse> CreateAsync(CreateProductRequest req)
    {
        ThrowIfInvalid(Validate(req.Name, req.Price, req.Stock));

        Category? category = null;
        if (req.CategoryUuid is not null)
        {
            category = await Db.Categories.FirstOrDefaultAsync(c => c.Uuid == req.CategoryUuid);
            if (category is null)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["categoryUuid"] = ["Category tidak ditemukan."]
                });
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

        Db.Products.Add(product);
        await Db.SaveChangesAsync();

        product.Category = category;
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid uuid, UpdateProductRequest req)
    {
        ThrowIfInvalid(Validate(req.Name, req.Price, req.Stock));

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Uuid == uuid);
        if (product is null)
            throw new NotFoundException($"Product '{uuid}' tidak ditemukan.");

        int? categoryId = null;
        if (req.CategoryUuid is not null)
        {
            var category = await Db.Categories.FirstOrDefaultAsync(c => c.Uuid == req.CategoryUuid);
            if (category is null)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["categoryUuid"] = ["Category tidak ditemukan."]
                });
            categoryId = category.Id;
        }

        product.Name = req.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        product.Price = req.Price;
        product.Stock = req.Stock;
        product.CategoryId = categoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await Db.SaveChangesAsync();
        await Db.Entry(product).Reference(p => p.Category).LoadAsync();
        return ToResponse(product);
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

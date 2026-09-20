using Microsoft.EntityFrameworkCore;
using cs_api_v1.Common.Exceptions;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Models;

namespace cs_api_v1.Services.Common;

// Template base untuk semua service tabel.
// Service konkret tinggal: mapping ToResponse + validasi + Create/Update.
// Get/Delete/pagination/exception sudah ditangani di sini.
public abstract class CrudService<TEntity>(AppDbContext db)
    where TEntity : BaseEntity
{
    protected readonly AppDbContext Db = db;
    protected virtual string EntityName => typeof(TEntity).Name;

    // Query dasar (no-tracking). Override kalau butuh Include default.
    protected virtual IQueryable<TEntity> Query() => Db.Set<TEntity>().AsNoTracking();

    // Ambil 1 entity by uuid, 404 kalau tidak ada.
    public virtual async Task<TEntity> GetEntityAsync(Guid uuid)
    {
        var entity = await Query().FirstOrDefaultAsync(e => e.Uuid == uuid);
        if (entity is null)
            throw new NotFoundException($"{EntityName} '{uuid}' tidak ditemukan.");
        return entity;
    }

    // Hapus by uuid. Override EnsureDeletableAsync untuk guard
    // (contoh: category yang masih dipakai product).
    public virtual async Task DeleteAsync(Guid uuid)
    {
        var entity = await Db.Set<TEntity>().FirstOrDefaultAsync(e => e.Uuid == uuid);
        if (entity is null)
            throw new NotFoundException($"{EntityName} '{uuid}' tidak ditemukan.");

        await EnsureDeletableAsync(entity);
        Db.Set<TEntity>().Remove(entity);
        await Db.SaveChangesAsync();
    }

    protected virtual Task EnsureDeletableAsync(TEntity entity) => Task.CompletedTask;

    protected static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    protected static PagedResponse<T> ToPaged<T>(IReadOnlyList<T> items, int total, int page, int pageSize) =>
        new(items, total, page, pageSize);

    protected static (int page, int pageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));
}

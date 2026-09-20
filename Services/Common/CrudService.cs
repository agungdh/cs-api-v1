using System.Text;
using Microsoft.EntityFrameworkCore;
using cs_api_v1.Common.Exceptions;
using cs_api_v1.Data;
using cs_api_v1.Dtos;
using cs_api_v1.Entities;

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

    // --- Cursor pagination (infinite scroll) ---
    // Keyset di Id int (selalu naik, deterministik), cursor opaque base64
    // supaya id internal tidak bocor ke FE. Urutan: terbaru dulu (id DESC).
    // Ambil limit+1 baris untuk tahu masih ada halaman berikut atau tidak.
    protected static async Task<CursorResponse<TResponse>> ToCursorPageAsync<TResponse>(
        IQueryable<TEntity> query,
        string? cursor,
        int limit,
        Func<TEntity, TResponse> map)
    {
        limit = Math.Clamp(limit, 1, 100);

        if (TryDecodeCursor(cursor, out var lastId))
            query = query.Where(e => e.Id < lastId);

        var entities = await query
            .OrderByDescending(e => e.Id)
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = entities.Count > limit;
        var items = entities.Take(limit).Select(map).ToList();
        string? nextCursor = hasMore ? EncodeCursor(entities[limit - 1].Id) : null;

        return new CursorResponse<TResponse>(items, nextCursor, hasMore);
    }

    protected static string EncodeCursor(int id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString()));

    protected static bool TryDecodeCursor(string? cursor, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(decoded, out id) && id > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

namespace cs_api_v1.Entities;

// Template untuk semua tabel: PK int identity (internal) + uuid (ke FE) + audit.
// 100 tabel berikutnya tinggal `: BaseEntity`, tidak perlu tulis ulang 4 field ini.
public abstract class BaseEntity
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

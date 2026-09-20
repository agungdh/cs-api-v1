namespace cs_api_v1.Dtos;

// Cursor pagination buat infinite scroll.
// FE: request pertama tanpa cursor, request berikut pakai NextCursor.
// Selesai kalau HasMore = false (NextCursor null).
public record CursorResponse<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

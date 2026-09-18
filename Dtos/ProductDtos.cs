namespace cs_api_v1.Dtos;

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

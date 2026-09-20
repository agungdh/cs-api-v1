namespace cs_api_v1.Dtos;

public record ProductResponse(
    Guid Uuid,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid? CategoryUuid,
    string? CategoryName,
    DateTime? CreatedAt,
    DateTime? UpdatedAt);

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid? CategoryUuid);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid? CategoryUuid);

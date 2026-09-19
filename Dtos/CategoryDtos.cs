namespace cs_api_v1.Dtos;

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateCategoryRequest(
    string Name,
    string? Description);

public record UpdateCategoryRequest(
    string Name,
    string? Description);

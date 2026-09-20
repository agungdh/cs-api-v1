namespace cs_api_v1.Common.Exceptions;

// Dilempar service, diterjemahkan ke status code oleh ApiExceptionHandler.
// Mirip ResponseStatusException / @ControllerAdvice di Spring.

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class ValidationException(Dictionary<string, string[]> errors)
    : Exception("Validation failed")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using cs_api_v1.Common.Exceptions;

namespace cs_api_v1.Middleware;

// Global exception -> ProblemDetails mapping.
// Setara @ControllerAdvice di Spring: service lempar, ini yang ubah jadi response.

public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, details) = exception switch
        {
            ValidationException vex => (
                StatusCodes.Status400BadRequest,
                (ProblemDetails)new ValidationProblemDetails(vex.Errors)
                {
                    Title = "Validation failed.",
                    Status = StatusCodes.Status400BadRequest,
                }),
            NotFoundException nex => (
                StatusCodes.Status404NotFound,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Not found.",
                    Detail = nex.Message,
                    Status = StatusCodes.Status404NotFound,
                }),
            ConflictException cex => (
                StatusCodes.Status409Conflict,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Conflict.",
                    Detail = cex.Message,
                    Status = StatusCodes.Status409Conflict,
                }),
            _ => (
                StatusCodes.Status500InternalServerError,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Internal server error.",
                    Status = StatusCodes.Status500InternalServerError,
                }),
        };

        httpContext.Response.StatusCode = status;
        // Cast ke object agar serializer pakai runtime type
        // (ValidationProblemDetails.errors ikut keluar).
        await httpContext.Response.WriteAsJsonAsync((object)details, cancellationToken);
        return true;
    }
}

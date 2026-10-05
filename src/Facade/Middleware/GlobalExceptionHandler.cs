using Domain.Common;
using Facade.Http;
using Microsoft.AspNetCore.Diagnostics;

namespace Facade.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var error = MapError(exception);

        if (error.Type == ErrorType.Failure)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request failed with {ErrorCode}: {Message}", error.Code, error.Message);
        }

        var problem = error.ToProblem(httpContext);
        await problem.ExecuteAsync(httpContext);
        return true;
    }

    private static Error MapError(Exception exception) => exception switch
    {
        DuplicateRecordException => Error.Conflict("data.duplicate_record", "A record with the same unique value already exists."),
        BadHttpRequestException => Error.Validation("request.malformed", "The request is malformed or contains invalid values."),
        _ => Error.Failure("server.unexpected_error", "An unexpected error occurred.")
    };
}

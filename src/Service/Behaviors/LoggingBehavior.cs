using System.Diagnostics;
using Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Service.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        stopwatch.Stop();
        LogOutcome(requestName, response, stopwatch.ElapsedMilliseconds);
        return response;
    }

    private void LogOutcome(string requestName, TResponse response, long elapsedMilliseconds)
    {
        if (response.IsSuccess)
        {
            logger.LogInformation("Handled {RequestName} successfully in {ElapsedMilliseconds} ms", requestName, elapsedMilliseconds);
            return;
        }

        logger.LogWarning("Handled {RequestName} with error {ErrorCode} ({ErrorType}) in {ElapsedMilliseconds} ms", requestName, response.Error!.Code, response.Error.Type, elapsedMilliseconds);
    }
}

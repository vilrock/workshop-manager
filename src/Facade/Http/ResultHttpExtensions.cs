using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Facade.Http;

public static class ResultHttpExtensions
{
    public const string TraceIdKey = "traceId";

    public const string ErrorCodeKey = "errorCode";

    public const string CorrelationIdKey = "correlationId";

    public static IResult ToHttpResult<TValue>(this Result<TValue> result, HttpContext http, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error!.ToProblem(http);

    public static IResult ToProblem(this Error error, HttpContext http)
    {
        var status = MapStatusCode(error.Type);
        var problem = ProblemDetailsFactory.Create(http, status, error.Type == ErrorType.Failure ? "An unexpected error occurred." : error.Message);
        problem.Extensions[ErrorCodeKey] = error.Code;

        if (error.ValidationErrors is not null)
        {
            problem.Extensions["errors"] = error.ValidationErrors;
        }

        return Results.Problem(problem);
    }

    public static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };
}

public static class ProblemDetailsFactory
{
    public static ProblemDetails Create(HttpContext http, int status, string? detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Type = $"https://httpstatuses.io/{status}",
            Detail = detail,
            Instance = http.Request.Path
        };

        Enrich(http, problem);
        return problem;
    }

    public static void Enrich(HttpContext http, ProblemDetails problem)
    {
        problem.Extensions[ResultHttpExtensions.TraceIdKey] = Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;

        if (http.Items[Middleware.CorrelationIdMiddleware.ItemKey] is string correlationId)
        {
            problem.Extensions[ResultHttpExtensions.CorrelationIdKey] = correlationId;
        }
    }
}

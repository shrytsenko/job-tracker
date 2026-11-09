using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.ErrorHandling;

/// <summary>
/// Turns "the client sent something wrong" exceptions into a 4xx ProblemDetails instead of a 500:
/// <list type="bullet">
/// <item>BadHttpRequestException: binding failures (e.g. ?status=NotAStatus). In Development the
/// framework throws these instead of returning 400, so without this handler they surface as 500.</item>
/// <item>ArgumentException: a domain guard rejected input that slipped past request validation.</item>
/// </list>
/// Anything else is left to the default handler (generic 500 ProblemDetails, no internals leaked).
/// </summary>
public sealed class BadRequestExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            BadHttpRequestException badRequest => badRequest.StatusCode,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => (int?)null,
        };

        if (statusCode is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode.Value;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = "Invalid request",
                Detail = exception.Message,
            },
        });
    }
}

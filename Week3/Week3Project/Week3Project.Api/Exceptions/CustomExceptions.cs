using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Week3Project.Api.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Handle expected client-side disconnects or timeouts gracefully
        if (exception is OperationCanceledException || exception is TaskCanceledException)
        {
            // Log as Warning because user cancellations are a normal operational event
            Log.Warning("Fulfillment request was cancelled by the client or interrupted by server shutdown.");

            httpContext.Response.StatusCode = 499; // Standard HTTP status code for 'Client Closed Request'
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Status = 499,
                Title = "Request Cancelled",
                Detail = "The order fulfillment pipeline operation was aborted."
            }, cancellationToken);

            return true; // Tells ASP.NET Core we fully handled this exception
        }

        // 2. Handle true unexpected system crashes (DB timeouts, null references, etc.)
        // Log as Error because this represents an actual technical issue that requires attention
        Log.Error(exception, "A fatal unhandled exception slipped out of the fulfillment core: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Execution Fault",
            Detail = "An unexpected error occurred while processing your request. The engine encountered an inner fault.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; 
    }
}
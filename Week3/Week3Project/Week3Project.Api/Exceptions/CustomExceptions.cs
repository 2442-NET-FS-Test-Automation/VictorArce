using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Week3Project.Api.Exceptions;

/// <summary>
/// Intercepts all unhandled exceptions thrown anywhere within the HTTP execution pipeline.
/// Normalizes error responses into clean, predictable JSON formats and preserves telemetry logs.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle the specified exception within the active HTTP request context contextually.
    /// </summary>
    /// <param name="httpContext">The active HTTP request/response environment wrapping the failed operation.</param>
    /// <param name="exception">The unhandled raw exception thrown by an underlying service or endpoint.</param>
    /// <param name="cancellationToken">Propagates notifications that operations should be canceled.</param>
    /// <returns>True if the exception has been completely resolved; False to pass it down to lower middleware.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // ------------------------------------------------------------------------
        // CASE 1: Handle expected client-side disconnects or timeouts gracefully
        // ------------------------------------------------------------------------
        // This fires when a user closes their browser tab, cancels a network request, 
        // or when the server triggers a clean shutdown sequence during an active task.
        if (exception is OperationCanceledException || exception is TaskCanceledException)
        {
            // Log as a Warning rather than an Error because user cancellations are 
            // completely normal operational events that do not indicate a system bug.
            Log.Warning("Fulfillment request was cancelled by the client or interrupted by server shutdown.");

            // Use the non-standard but universally recognized Nginx status code 499 (Client Closed Request).
            // This prevents automated APM alarms (like Datadog/NewRelic) from flagging normal user exits as system crashes.
            httpContext.Response.StatusCode = 499; 
            
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Status = 499,
                Title = "Request Cancelled",
                Detail = "The order fulfillment pipeline operation was aborted."
            }, cancellationToken);

            // Returning true tells ASP.NET Core that this exception has been caught and formatted successfully.
            // The runtime will stop bubbled propagation right here.
            return true; 
        }

        // ------------------------------------------------------------------------
        // CASE 2: Handle true unexpected system crashes (DB deadlocks, NULL faults, etc.)
        // ------------------------------------------------------------------------
        // This block catches the true system bugs that require a developer's attention to diagnose and fix.
        
        // Log explicitly as a Fatal/Severe Error, preserving the complete stack trace for later inspection.
        Log.Error(exception, "A fatal unhandled exception slipped out of the fulfillment core: {Message}", exception.Message);

        // Standardize the API breakdown response using the official RFC 7807 'Problem Details' specification layout.
        // This gives downstream frontends/consumers a uniform error footprint without exposing sensitive system stack traces.
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError, // Mark explicitly as a standard 500 Server Fault
            Title = "Execution Fault",
            Detail = "An unexpected error occurred while processing your request. The engine encountered an inner fault.",
            Instance = httpContext.Request.Path // Provide the exact route path where the error occurred
        };

        // Match the raw response payload header status code to the inner JSON model data structure values
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        
        // Push the error response down the wire to the calling client interface
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to indicate the HTTP channel has been cleanly closed and structured error response delivered
        return true; 
    }
}
using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Common.Exceptions;

namespace MiPresupuesto.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message, errors) = exception switch
        {
            AppException appException => (
                appException.StatusCode,
                appException.ErrorCode,
                appException.Message,
                appException.Errors),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "database_conflict",
                "Los datos entran en conflicto con un registro existente.",
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "Ocurrió un error inesperado.",
                null)
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled error. TraceId: {TraceId}", context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                "Request rejected with {StatusCode} ({Code}): {Message}. TraceId: {TraceId}",
                statusCode,
                code,
                message,
                context.TraceIdentifier);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            error = new
            {
                code,
                message,
                errors,
                traceId = context.TraceIdentifier
            }
        });
    }
}

using Microsoft.AspNetCore.Diagnostics;

namespace PROTOTYPE_backend.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IConfiguration _configuration;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Kivétel történt: {Message}", exception.Message);

        var exceptionName = exception.GetType().Name;
        var section = _configuration.GetSection($"ErrorCatalog:Exceptions:{exceptionName}");

        if (!section.Exists())
        {
            section = _configuration.GetSection("ErrorCatalog:Exceptions:Default");
        }

        var statusCode = section.GetValue<int>("StatusCode", StatusCodes.Status500InternalServerError);
        var errorCode = section["Code"] ?? "SZERVER_HIBA";
        // Uses the JSON message if defined (e.g., for 500), otherwise falls back to the thrown exception's message
        var message = section["Message"] ?? exception.Message;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                message,
                code = errorCode
            }
        }, cancellationToken);

        return true;
    }
}
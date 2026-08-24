using FluentValidation;
using LogisticsSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsSystem.Api.Common.Extensions
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                exception.Message);

            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            var correlationId = httpContext.Items["CorrelationId"]?.ToString()
                ?? httpContext.TraceIdentifier;
            problemDetails.Extensions["correlationId"] = correlationId;

            switch (exception)
            {
                case ValidationException validationException:

                    problemDetails.Title = "Validation Failed";
                    problemDetails.Status =
                        StatusCodes.Status400BadRequest;

                    problemDetails.Detail =
                        "One or more validation errors occurred.";

                    problemDetails.Extensions["errors"] =
                        validationException.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(
                                    x => x.ErrorMessage));

                    httpContext.Response.StatusCode =
                        StatusCodes.Status400BadRequest;

                    break;

                case KeyNotFoundException:

                    problemDetails.Title =
                        "Resource Not Found";

                    problemDetails.Status =
                        StatusCodes.Status404NotFound;

                    problemDetails.Detail =
                        exception.Message;

                    httpContext.Response.StatusCode =
                        StatusCodes.Status404NotFound;

                    break;

                case UnauthorizedAccessException:

                    problemDetails.Title =
                        "Access Denied";

                    problemDetails.Status =
                        StatusCodes.Status403Forbidden;

                    problemDetails.Detail =
                        exception.Message;

                    httpContext.Response.StatusCode =
                        StatusCodes.Status403Forbidden;

                    break;

                case DomainException:

                    problemDetails.Title =
                        "Domain Rule Violated";

                    problemDetails.Status =
                        StatusCodes.Status422UnprocessableEntity;

                    problemDetails.Detail =
                        exception.Message;

                    httpContext.Response.StatusCode =
                        StatusCodes.Status422UnprocessableEntity;

                    break;

                case InvalidOperationException:

                    problemDetails.Title =
                        "Business Rule Conflict";

                    problemDetails.Status =
                        StatusCodes.Status409Conflict;

                    problemDetails.Detail =
                        exception.Message;

                    httpContext.Response.StatusCode =
                        StatusCodes.Status409Conflict;

                    break;

                default:

                    problemDetails.Title =
                        "Server Error";

                    problemDetails.Status =
                        StatusCodes.Status500InternalServerError;

                    problemDetails.Detail =
                        "An unexpected error occurred.";

                    httpContext.Response.StatusCode =
                        StatusCodes.Status500InternalServerError;

                    break;
            }

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);

            return true;
        }
    }
}
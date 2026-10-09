using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Services;

namespace WebAPIDevSecOpsScallingSDD.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(environment);
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }

                await WriteErrorAsync(context, ex).ConfigureAwait(false);
            }
        }

        private async Task WriteErrorAsync(HttpContext context, Exception ex)
        {
            var (status, message) = Map(ex);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            var body = new ErrorResponse
            {
                Error = message,
                Status = status,
                TraceId = context.TraceIdentifier,
            };
            if (!_environment.IsProduction())
            {
                body.Detail = ex.Message;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(body)).ConfigureAwait(false);
        }

        private static (int Status, string Message) Map(Exception ex)
        {
            return ex switch
            {
                NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
                ForbiddenAccessException forbidden => (StatusCodes.Status403Forbidden, forbidden.Message),
                ConcurrencyConflictException => (StatusCodes.Status409Conflict, "El recurso fue modificado por otro proceso."),
                ValidationException validation => (StatusCodes.Status422UnprocessableEntity, validation.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, "La solicitud contiene valores inválidos."),
                TimeoutException => (StatusCodes.Status408RequestTimeout, "La solicitud excedió el tiempo máximo."),
                OperationCanceledException => (StatusCodes.Status408RequestTimeout, "La solicitud excedió el tiempo máximo."),
                _ => (StatusCodes.Status500InternalServerError, "Un error interno ha ocurrido en el servidor."),
            };
        }
    }
}

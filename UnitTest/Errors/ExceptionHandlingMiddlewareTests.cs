using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WebAPIDevSecOpsScallingSDD.Dtos;
using WebAPIDevSecOpsScallingSDD.Middleware;
using WebAPIDevSecOpsScallingSDD.Services;

namespace UnitTest.Errors
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public void NullDependenciesThrowArgumentNull()
        {
            var env = new StubEnvironment(Environments.Production);

            Assert.Throws<ArgumentNullException>(() => new ExceptionHandlingMiddleware(null!, env));
            Assert.Throws<ArgumentNullException>(() => new ExceptionHandlingMiddleware(_ => Task.CompletedTask, null!));
        }

        [Fact]
        public async Task NullContextThrowsArgumentNull()
        {
            var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, new StubEnvironment(Environments.Production));

            await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!));
        }

        [Fact]
        public async Task NoExceptionPassesThroughWithoutBody()
        {
            var context = CreateContext();
            var middleware = new ExceptionHandlingMiddleware(_ =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return Task.CompletedTask;
            }, new StubEnvironment(Environments.Production));

            await middleware.InvokeAsync(context);

            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
            Assert.Equal(0, ((MemoryStream)context.Response.Body).Length);
        }

        [Theory]
        [InlineData(typeof(NotFoundException), 404, "Cliente '5' no encontrado.")]
        [InlineData(typeof(ForbiddenAccessException), 403, "El detalle no pertenece al usuario autenticado.")]
        [InlineData(typeof(ConcurrencyConflictException), 409, "El recurso fue modificado por otro proceso.")]
        [InlineData(typeof(ValidationException), 422, "Producto '1' no existe.")]
        [InlineData(typeof(ArgumentException), 400, "La solicitud contiene valores inválidos.")]
        [InlineData(typeof(TimeoutException), 408, "La solicitud excedió el tiempo máximo.")]
        [InlineData(typeof(OperationCanceledException), 408, "La solicitud excedió el tiempo máximo.")]
        [InlineData(typeof(InvalidOperationException), 500, "Un error interno ha ocurrido en el servidor.")]
        public async Task MappingTableReturnsExpectedStatusAndBody(Type exceptionType, int status, string error)
        {
            var context = CreateContext();
            var middleware = new ExceptionHandlingMiddleware(_ => throw CreateException(exceptionType), new StubEnvironment(Environments.Production));

            await middleware.InvokeAsync(context);

            Assert.Equal(status, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            var body = ReadBody(context);
            Assert.Equal(error, body.Error);
            Assert.Equal(status, body.Status);
            Assert.Equal("trace-1", body.TraceId);
            Assert.Null(body.Detail);
        }

        [Fact]
        public async Task NonProductionIncludesDetail()
        {
            var context = CreateContext();
            var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("boom"), new StubEnvironment(Environments.Development));

            await middleware.InvokeAsync(context);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("boom", ReadBody(context).Detail);
        }

        [Fact]
        public async Task ProductionOmitsDetail()
        {
            var context = CreateContext();
            var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("boom"), new StubEnvironment(Environments.Production));

            await middleware.InvokeAsync(context);

            Assert.Null(ReadBody(context).Detail);
        }

        [Fact]
        public async Task StartedResponseRethrows()
        {
            var features = new FeatureCollection();
            features.Set<IHttpResponseFeature>(new StartedResponseFeature());
            var context = new DefaultHttpContext(features);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("late"),
                new StubEnvironment(Environments.Production));

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        }

        private static Exception CreateException(Type type)
        {
            if (type == typeof(NotFoundException))
            {
                return new NotFoundException("Cliente '5' no encontrado.");
            }

            if (type == typeof(ForbiddenAccessException))
            {
                return new ForbiddenAccessException("El detalle no pertenece al usuario autenticado.");
            }

            if (type == typeof(ConcurrencyConflictException))
            {
                return new ConcurrencyConflictException();
            }

            if (type == typeof(ValidationException))
            {
                return new ValidationException("Producto '1' no existe.");
            }

            if (type == typeof(ArgumentException))
            {
                return new ArgumentException("bad-arg");
            }

            if (type == typeof(TimeoutException))
            {
                return new TimeoutException("timed out");
            }

            if (type == typeof(OperationCanceledException))
            {
                return new OperationCanceledException("canceled");
            }

            return new InvalidOperationException("boom");
        }

        private static DefaultHttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "trace-1";
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static ErrorResponse ReadBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<ErrorResponse>(json)!;
        }

        private sealed class StartedResponseFeature : IHttpResponseFeature
        {
            public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

            public string? ReasonPhrase { get; set; }

            public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

            public Stream Body { get; set; } = Stream.Null;

            public bool HasStarted => true;

            public void OnStarting(Func<object, Task> callback, object state)
            {
            }

            public void OnCompleted(Func<object, Task> callback, object state)
            {
            }
        }

        private sealed class StubEnvironment : IHostEnvironment
        {
            public StubEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "Test";

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System;
using System.Threading;
using WebAPIDevSecOpsScallingSDD;

var builder = WebApplication.CreateBuilder(args);

WebApiServiceCollectionExtensions.AddWebApiDevSecOpsServices(builder.Services, builder.Configuration);

var app = builder.Build();

WebApiApplicationExtensions.UseWebApiDevSecOpsPipeline(app);

app.MapGet("/ping", () => "pong");

await app.RunAsync().ConfigureAwait(false);

/// <summary>Program class exposed for WebApplicationFactory tests.</summary>
#pragma warning disable S1118 // Top-level entry-point class cannot be static; this partial only exists for WebApplicationFactory.
#pragma warning disable CA1515 // Referenced by integration/test assemblies via WebApplicationFactory.
public partial class Program
{
}
#pragma warning restore S1118
#pragma warning restore CA1515

namespace WebAPIDevSecOpsScallingSDD
{
    /// <summary>DI registration for the web API foundation.</summary>
    internal static class WebApiServiceCollectionExtensions
    {
        /// <summary>Registers the services required by the web API foundation.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        public static void AddWebApiDevSecOpsServices(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddOpenApi();
            services.AddAuthentication(options => options.DefaultChallengeScheme = "Anonymous")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Services.AnonymousChallengeHandler>("Anonymous", _ => { });
            services.AddAuthorization();
            services.AddAuthorizationBuilder().AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            services.AddMemoryCache();
            services.AddScoped<Services.ICliClienteService, Services.CliClienteService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.CliClienteCreateDto>, Validators.CliClienteCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.CliClienteUpdateDto>, Validators.CliClienteUpdateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.CliClienteDeleteDto>, Validators.CliClienteDeleteValidator>();
            services.AddScoped<Services.IEmpEmpleadoService, Services.EmpEmpleadoService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.EmpEmpleadoCreateDto>, Validators.EmpEmpleadoCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.EmpEmpleadoUpdateDto>, Validators.EmpEmpleadoUpdateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.EmpEmpleadoDeleteDto>, Validators.EmpEmpleadoDeleteValidator>();
            services.AddScoped<Services.IProProductoService, Services.ProProductoService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.ProProductoCreateDto>, Validators.ProProductoCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.ProProductoUpdateDto>, Validators.ProProductoUpdateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.ProProductoDeleteDto>, Validators.ProProductoDeleteValidator>();
            services.AddScoped<Services.IVenCatEstadoService, Services.VenCatEstadoService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.VenCatEstadoCreateDto>, Validators.VenCatEstadoCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.VenCatEstadoUpdateDto>, Validators.VenCatEstadoUpdateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.VenCatEstadoDeleteDto>, Validators.VenCatEstadoDeleteValidator>();
            services.AddScoped<Services.ISegUsuarioPasswordHasher, Services.FakeSegUsuarioPasswordHasher>();
            services.AddScoped<Services.ILoginService, Services.LoginService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.LoginRequest>, Validators.LoginRequestValidator>();
            services.AddScoped<Services.ITotpService, Services.FakeTotpService>();
            services.AddScoped<Services.ILogin2FaService, Services.Login2FaService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.Login2FaVerifyRequest>, Validators.Login2FaVerifyRequestValidator>();
            services.AddScoped<Services.IRefreshTokenService, Services.RefreshTokenService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.RefreshRequest>, Validators.RefreshRequestValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.LogoutRequest>, Validators.LogoutRequestValidator>();
            services.AddScoped<Services.ISegUsuarioService, Services.SegUsuarioService>();
            services.AddScoped<Services.IVentaService, Services.VentaService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.VenVentaCreateDto>, Validators.VenVentaCreateValidator>();
            services.AddScoped<Services.IVentaDetalleService, Services.VentaDetalleService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.VenVentaDetalleCreateDto>, Validators.VenVentaDetalleCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.VenVentaDetalleDeleteDto>, Validators.VenVentaDetalleDeleteValidator>();
            services.AddScoped<Services.IPedidoEventPublisher, Services.FakePedidoEventPublisher>();
            services.AddScoped<Services.IVentasPedidoService, Services.VentasPedidoService>();
            services.AddScoped<Services.StockValidatorConsumer>();
            services.AddScoped<FluentValidation.IValidator<Dtos.PedidoCreateDto>, Validators.PedidoCreateValidator>();
            services.AddScoped<Services.IVentasPagoService, Services.VentasPagoService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.PagoCreateDto>, Validators.PagoCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.SegUsuarioCreateDto>, Validators.SegUsuarioCreateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.SegUsuarioUpdateDto>, Validators.SegUsuarioUpdateValidator>();
            services.AddScoped<FluentValidation.IValidator<Dtos.SegUsuarioDeleteDto>, Validators.SegUsuarioDeleteValidator>();
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
            }).AddMvc().AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(serviceProvider =>
            {
                var redisConfig = serviceProvider.GetRequiredService<IConfiguration>().GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
                var configurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConfig.ConnectionString);
                configurationOptions.AbortOnConnectFail = false;
                configurationOptions.ConnectTimeout = 2000;
                configurationOptions.SyncTimeout = 1000;
                configurationOptions.ReconnectRetryPolicy = new StackExchange.Redis.ExponentialRetry(5000);
                return StackExchange.Redis.ConnectionMultiplexer.Connect(configurationOptions);
            });
            services.AddSingleton<Services.ICacheService, Services.CacheService>();
            services.AddDbContext<Context.AppDbContext>((serviceProvider, options) =>
            {
                var runtimeConfig = serviceProvider.GetRequiredService<IConfiguration>();
                if (runtimeConfig.GetValue("UseInMemoryDatabase", true))
                {
                    options.UseInMemoryDatabase("WebApiDevSecOpsScallingSDD");
                }
                else
                {
                    var connectionString = runtimeConfig.GetConnectionString("Default");
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException("ConnectionStrings:Default is required when UseInMemoryDatabase is false.");
                    }

                    options.UseSqlServer(connectionString);
                }
            });
            var liveTags = new[] { "live" };
            var readyTags = new[] { "ready" };
            services.AddHealthChecks()
                .AddCheck<Services.AssemblyIntegrityCheck>("assembly-integrity", tags: liveTags)
                .AddCheck<Services.RedisHealthCheck>("redis", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, tags: readyTags);
            services.AddCors(options =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                options.AddDefaultPolicy(policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
            });
        }
    }

    /// <summary>Middleware pipeline for the web API foundation.</summary>
    internal static class WebApiApplicationExtensions
    {
        /// <summary>Adds the foundational middleware in the prescribed order.</summary>
        /// <param name="app">The application builder.</param>
        public static void UseWebApiDevSecOpsPipeline(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"error\": \"Un error interno ha ocurrido en el servidor.\"}").ConfigureAwait(false);
                    });
                });
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseCors();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

            if (app.Configuration.GetValue("EnableProviderStates", false) && !app.Environment.IsProduction())
            {
                app.MapPost("/provider-states", async (ProviderStateRequest request, Context.AppDbContext db, CancellationToken cancellationToken) =>
                {
                    if (request is null || string.IsNullOrWhiteSpace(request.State))
                    {
                        return Results.BadRequest(new { error = "State is required." });
                    }

                    var applied = await Context.DatabaseSeeder.ApplyStateAsync(db, request.State, cancellationToken).ConfigureAwait(false);
                    return applied ? Results.Ok(new { state = request.State }) : Results.BadRequest(new { error = $"Unknown provider state '{request.State}'." });
                });
            }
        }

        internal sealed class ProviderStateRequest
        {
            public string State { get; set; } = string.Empty;
        }
    }
}

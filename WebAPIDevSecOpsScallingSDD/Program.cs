using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
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
            // (04-01) JWT HS256 real tras flag Authentication:UseJwtBearer (default true).
            // false = esquema Anonymous legacy (solo pruebas locales). Integration/Contract fuerzan el esquema "Test" vía ConfigureTestServices.
            var useJwtBearer = configuration.GetValue("Authentication:UseJwtBearer", true);
            if (useJwtBearer)
            {
                var signingKey = configuration["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(signingKey))
                {
                    signingKey = Services.JwtTokenService.FallbackKey;
                }

                var signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
                if (signingKeyBytes.Length < 32)
                {
                    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HS256.");
                }

                var tokenIssuer = configuration["Jwt:Issuer"];
                if (string.IsNullOrWhiteSpace(tokenIssuer))
                {
                    tokenIssuer = Services.JwtTokenService.FallbackIssuer;
                }

                var tokenAudience = configuration["Jwt:Audience"];
                if (string.IsNullOrWhiteSpace(tokenAudience))
                {
                    tokenAudience = Services.JwtTokenService.FallbackAudience;
                }

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
                        ValidateIssuer = true,
                        ValidIssuer = tokenIssuer,
                        ValidateAudience = true,
                        ValidAudience = tokenAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var jti = context.Principal?.FindFirstValue("jti");
                            if (string.IsNullOrWhiteSpace(jti))
                            {
                                context.Fail("Missing jti claim.");
                                return;
                            }

                            var cache = context.HttpContext.RequestServices.GetRequiredService<Services.ICacheService>();
                            var revoked = await cache.GetAsync<string>("blacklist:", jti).ConfigureAwait(false);
                            if (string.Equals(revoked, "revoked", StringComparison.Ordinal))
                            {
                                context.Fail("Token revoked.");
                            }
                        },
                    };
                });
            }
            else
            {
                services.AddAuthentication(options => options.DefaultChallengeScheme = "Anonymous")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Services.AnonymousChallengeHandler>("Anonymous", _ => { });
            }
            services.AddAuthorization();
            services.AddAuthorizationBuilder().AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            services.AddMemoryCache();
            // NOTE (03-09): AddDataProtection sin persistencia usa keyring efímero — válido en dev/test; en prod persistir claves (fuera de 03-09).
            services.AddDataProtection();
            services.AddScoped<Services.ITwoFactorSecretProtector, Services.TwoFactorSecretProtector>();
            services.AddScoped<Services.ITotpProvisioner, Services.OtpNetTotpService>();
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
            // (04-02) Hasher real Argon2id (64MB/3 iter) + fallback BCrypt solo migracion.
            // PasswordHasher: solo parametros de coste, sin secretos.
            services.Configure<Services.PasswordHasherOptions>(configuration.GetSection("PasswordHasher"));
            services.AddScoped<Services.ISegUsuarioPasswordHasher>(serviceProvider =>
            {
                var runtimeConfig = serviceProvider.GetRequiredService<IConfiguration>();
                var options = runtimeConfig.GetSection("PasswordHasher").Get<Services.PasswordHasherOptions>() ?? new Services.PasswordHasherOptions();
                return new Services.Argon2IdSegUsuarioPasswordHasher(options);
            });
            // (04-02) Bloqueo persistente 15 min fuera de la caché efímera.
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddScoped<Services.ILoginLockoutStore, Services.EfLoginLockoutStore>();
            services.AddScoped<Services.ILoginService, Services.LoginService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.LoginRequest>, Validators.LoginRequestValidator>();
            services.AddScoped<Services.ITotpService, Services.OtpNetTotpService>();
            services.AddScoped<Services.ILogin2FaService, Services.Login2FaService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.Login2FaVerifyRequest>, Validators.Login2FaVerifyRequestValidator>();
            services.AddScoped<Services.ITwoFactorService, Services.TwoFactorService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.TwoFactorVerifyRequest>, Validators.TwoFactorVerifyRequestValidator>();
            services.AddSingleton<Services.IJwtTokenService, Services.JwtTokenService>();
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
            services.AddScoped<Services.IVentasFacturaService, Services.VentasFacturaService>();
            services.AddScoped<Services.IVentasDashboardService, Services.VentasDashboardService>();
            services.AddScoped<FluentValidation.IValidator<Dtos.DashboardFilterDto>, Validators.DashboardFilterValidator>();
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
            // (04-03) HSTS 365d solo MaxAge, sin IncludeSubDomains/preload. Se aplica en UseHsts() no-Dev.
            services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
            // (04-04) Rate limiting: 5 policies SlidingWindow/concurrencia por IP; 429 uniforme en OnRejected.
            // Opciones vía IOptionsMonitor con resolución lazy en runtime (misma lección que Redis/DbContext:
            // builder.Configuration aún no incluye los overrides de WebApplicationFactory durante el registro).
            // Relajación solo perf vía env PERF_RATELIMIT_MULTIPLIER (entero >1 multiplica todos los límites).
            services.AddOptions<Services.RateLimitOptions>()
                .BindConfiguration(Services.RateLimitOptions.SectionName)
                .PostConfigure(options =>
                {
                    var multiplierRaw = Environment.GetEnvironmentVariable("PERF_RATELIMIT_MULTIPLIER");
                    if (int.TryParse(multiplierRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rateLimitMultiplier) && rateLimitMultiplier > 1)
                    {
                        options.ApplyMultiplier(rateLimitMultiplier);
                    }
                });

            services.AddRateLimiter(limiter =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    var rejected = new Dtos.ErrorResponse
                    {
                        Error = "Demasiadas solicitudes. Intente de nuevo más tarde.",
                        Status = StatusCodes.Status429TooManyRequests,
                        TraceId = context.HttpContext.TraceIdentifier,
                    };
                    await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(rejected), cancellationToken).ConfigureAwait(false);
                };
                limiter.AddPolicy(Services.RateLimitOptions.LoginPolicyName, context => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: PartitionKey(context),
                    factory: _ => SlidingWindow(Options(context).LoginPermitLimit, Options(context).LoginWindowSeconds, 5)));
                limiter.AddPolicy(Services.RateLimitOptions.Login2faPolicyName, context => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: PartitionKey(context),
                    factory: _ => SlidingWindow(Options(context).Login2faPermitLimit, Options(context).Login2faWindowSeconds, 5)));
                limiter.AddPolicy(Services.RateLimitOptions.GlobalPolicyName, context => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: PartitionKey(context),
                    factory: _ => SlidingWindow(Options(context).GlobalPermitLimit, Options(context).GlobalWindowSeconds, 4)));
                limiter.AddPolicy(Services.RateLimitOptions.AdminPolicyName, context => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: PartitionKey(context),
                    factory: _ => SlidingWindow(Options(context).AdminPermitLimit, Options(context).AdminWindowSeconds, 4)));
                limiter.AddPolicy(Services.RateLimitOptions.ConcurrentWritesPolicyName, context => RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: PartitionKey(context),
                    factory: _ => new ConcurrencyLimiterOptions { PermitLimit = SafePermit(Options(context).ConcurrentWritesPermitLimit), QueueLimit = 0 }));
            });
        }

        private static Services.RateLimitOptions Options(HttpContext context) =>
            context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Services.RateLimitOptions>>().CurrentValue;

        private static string PartitionKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private static int SafePermit(int value) => Math.Max(1, value);

        private static int SafeWindow(int seconds) => Math.Max(1, seconds);

        private static SlidingWindowRateLimiterOptions SlidingWindow(int permitLimit, int windowSeconds, int segments)
        {
            return new SlidingWindowRateLimiterOptions
            {
                PermitLimit = SafePermit(permitLimit),
                Window = TimeSpan.FromSeconds(SafeWindow(windowSeconds)),
                SegmentsPerWindow = segments,
                QueueLimit = 0,
            };
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
            // (04-03) SecurityHeaders lo mas externo: cubre DeveloperExceptionPage y errores 403/500 via OnStarting. Orden 01-02 intacto (aditivo).
            app.UseMiddleware<Middleware.SecurityHeadersMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

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

            // (04-04) RateLimiter antes de Auth (orden constitucional); solo actúa en endpoints con [EnableRateLimiting].
            app.UseRateLimiter();

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

                // NOTE (03-16): sondas deterministas para ErrorHandlingTests (403/408/500); solo no-prod.
                Func<string> probeTimeout = static () => throw new TimeoutException("Probe timeout.");
                Func<string> probeError = static () => throw new InvalidOperationException("Probe error.");
                Func<string> probeForbidden = static () => throw new Services.ForbiddenAccessException("Probe forbidden.");
                app.MapGet("/api/v1/probe/timeout", probeTimeout);
                app.MapGet("/api/v1/probe/error", probeError);
                app.MapGet("/api/v1/probe/forbidden", probeForbidden);
            }
        }

        internal sealed class ProviderStateRequest
        {
            public string State { get; set; } = string.Empty;
        }
    }
}

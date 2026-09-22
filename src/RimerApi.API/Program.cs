using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using RimerApi.Application;
using RimerApi.Application.Interfaces;
using RimerApi.API.Hubs;
using RimerApi.API.Providers;
using RimerApi.API.Services;
using RimerApi.Infrastructure;
using RimerApi.Infrastructure.Services;
using RimerApi.Infrastructure.Data.Seed;
using RimerApi.API.Middleware;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

// ── Bootstrap Serilog ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting RimerApi...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (full configuration from appsettings) ──────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // ══════════════════════════════════════════════════════════════
    // SERVICES
    // ══════════════════════════════════════════════════════════════

    // ── Layer registrations (1 line each) ──────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── System Protection Background Services ──────────────────────
    builder.Services.AddHostedService<SystemProtectionMonitor>();
    builder.Services.AddHostedService<RimerApi.Infrastructure.WebSockets.WebSocketBroadcastLoop>();

    // ── OpenTelemetry & Metrics (Observability) ────────────────────
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("RimerApi"))
                .AddMeter("RimerApi.AuditLogging")   // Custom AuditLog Queue Worker metrics
                .AddMeter("RimerApi.SystemProtection") // Load Shedding Metrics
                .AddAspNetCoreInstrumentation()      // Standard HTTP requests metrics
                .AddHttpClientInstrumentation()      // Outbound HTTP call metrics
                .AddRuntimeInstrumentation()         // GC/Memory/LOH metrics
                .AddPrometheusExporter();            // Expose via /metrics endpoint
        });

    // ── JWT Authentication ─────────────────────────────────────────
    // Secret key resolution order: Environment variable > appsettings.Development.json > appsettings.json
    // In production, set: JwtSettings__SecretKey=<your-secret> or JwtSettings:SecretKey via vault
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    bool isInvalidKey = string.IsNullOrEmpty(secretKey) || secretKey.Contains("CHANGE_ME") || secretKey.Length < 32;
    if (isInvalidKey)
    {
        Log.Warning("JWT SecretKey is missing, insecure, or contains 'CHANGE_ME'. Minimum 32 characters required.");
        Log.Warning("Falling back to a default secure key. Please configure a proper key in your environment!");
        
        // Define a secure default fallback value >= 32 characters
        secretKey = "RimerApi_Default_Secure_Fallback_Key_Do_Not_Use_In_Prod_!!!";
    }

    // ── Forwarded Headers (Reverse Proxy / Load Balancer Support) ──
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Optionally trust all proxies in development/internal networks, otherwise specify KnownProxies
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ── Rate Limiting ──────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Advanced Identity / IP Detection for Partitioning Key
        static string GetClientIdentifier(HttpContext context)
        {
            // Try to extract UserId if user is authenticated
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? context.User?.FindFirst("sub")?.Value;
                         
            if (!string.IsNullOrEmpty(userId))
            {
                return $"User_{userId}";
            }

            // Fallback to IP address (UseForwardedHeaders middleware resolves X-Forwarded-For securely)
            return $"IP_{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        options.OnRejected = async (context, token) =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            string clientId = GetClientIdentifier(context.HttpContext);
            
            logger.LogWarning("Rate limit triggered for {ClientId} on {Path}", clientId, context.HttpContext.Request.Path);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfter = "unknown";
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
            {
                retryAfter = retryAfterValue.TotalSeconds.ToString("F0");
            }

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later.",
                retryAfterSeconds = retryAfter
            }, cancellationToken: token);
        };

        // Global Burst-enabled Token Bucket (10000 req/min — elevated for load testing; production: 100)
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            return RateLimitPartition.GetTokenBucketLimiter(GetClientIdentifier(context), _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100000, //performans testi 400 rps e çıkarmak için gerekli
                TokensPerPeriod = 100000, //performans testi 400 rps e çıkarmak için gerekli
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        // Specific Login Policy: Token Bucket (Burst allowed up to 5)
        options.AddPolicy("LoginPolicy", context =>
        {
            return RateLimitPartition.GetTokenBucketLimiter(GetClientIdentifier(context), _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });

        // Specific Register Policy: Burst up to 3
        options.AddPolicy("RegisterPolicy", context =>
        {
            return RateLimitPartition.GetTokenBucketLimiter(GetClientIdentifier(context), _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 3,
                TokensPerPeriod = 3,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/notifications"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Log.Warning("JWT challenge issued for {Path}", context.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

    // ── Authorization ──────────────────────────────────────────────
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
        options.AddPolicy("AllRoles", policy => policy.RequireRole("Student", "Staff", "Admin"));
    });

    // ── Controllers ────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // ── SignalR & Notifications ─────────────────────────────────────
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
    builder.Services.AddScoped<INotificationClientService, SignalRNotificationService>();

    // ── Hangfire Background Jobs ────────────────────────────────────
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
        
    builder.Services.AddHangfireServer(options => 
    {
        options.WorkerCount = 1;
    });

    // ── Swagger with JWT support ───────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Rimer API — Rectorate Communication Center",
            Version = "v1",
            Description = "API for managing communication tickets between students/staff and the university rectorate.",
            Contact = new OpenApiContact
            {
                Name = "Rimer Support",
                Email = "support@rimer.edu"
            }
        });

        // JWT Bearer token input in Swagger UI
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token. Example: eyJhbGciOiJIUzI1NiIs..."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── CORS ───────────────────────────────────────────────────────
    var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
        ?? ["http://localhost:3000"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCors", policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ══════════════════════════════════════════════════════════════
    // MIDDLEWARE PIPELINE
    // ══════════════════════════════════════════════════════════════

    var app = builder.Build();

    // ── Global exception handler (first in pipeline) ───────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // ── Serilog request logging ────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    });

    // ── Swagger (all environments for now) ─────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rimer API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Rimer API — Swagger";
    });

    // ── Standard middleware order ───────────────────────────────────
    app.UseWebSockets();
    app.UseMiddleware<WebSocketMiddleware>();
    
    app.UseForwardedHeaders();
    app.UseHttpsRedirection();
    app.UseRateLimiter();

    // ── Adaptive Load Shedding (Highest priority in pipeline after raw connection/rate limiting) ──
    app.UseMiddleware<AdaptiveLoadSheddingMiddleware>();

    app.UseCors("DefaultCors");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<NotificationHub>("/hub/notifications");

    // ── OpenTelemetry Prometheus Endpoint ─────────────────────────
    app.MapPrometheusScrapingEndpoint("/metrics");
    
    // ── Hangfire Dashboard ─────────────────────────────────────────
    app.UseHangfireDashboard("/hangfire");

    // ── Configure Recurring Jobs ───────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<RimerApi.Infrastructure.Services.AuditRetentionJob>(
            "AuditRetention_Archival_And_Cleanup",
            job => job.ProcessRetentionAsync(3, 7),
            "0 */6 * * *"); // Every 6 hours

        recurringJobManager.AddOrUpdate<RimerApi.Infrastructure.Services.AuditLogRecoveryJob>(
            "AuditLog_Stuck_Records_Recovery",
            job => job.RecoverStuckLogsAsync(),
            "*/10 * * * *"); // Every 10 minutes
    }

    // ── Seed database ──────────────────────────────────────────────
    await DataSeeder.SeedAsync(app.Services);

    // ── Run ────────────────────────────────────────────────────────
    Log.Information("RimerApi is running on {Urls}", string.Join(", ",
        app.Urls.Any() ? app.Urls : ["https://localhost:5001"]));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

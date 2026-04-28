using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SqlTrainer.Api.Auth;
using SqlTrainer.Api.Controllers;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ PRODUCTION CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                    return false;

                return origin.Equals("https://sqltraining.up.railway.app", StringComparison.OrdinalIgnoreCase)
                    || origin.EndsWith(".up.railway.app", StringComparison.OrdinalIgnoreCase)
                    || origin.Equals("http://localhost:5173", StringComparison.OrdinalIgnoreCase)
                    || origin.Equals("https://localhost:5173", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Railway / proxy fix
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger (dev only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "SqlTrainer.Api", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Írd be így: Bearer {token}"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RunnerOptions>(builder.Configuration.GetSection("Runner"));

var appConn = builder.Configuration.GetConnectionString("AppDb");

if (string.IsNullOrWhiteSpace(appConn))
    throw new InvalidOperationException("Hiányzik a ConnectionStrings:AppDb beállítás.");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var configured = builder.Configuration["Database:ServerVersion"];
    ServerVersion serverVersion;

    if (!string.IsNullOrWhiteSpace(configured))
    {
        serverVersion = ServerVersion.Parse(configured);
    }
    else
    {
        try
        {
            serverVersion = ServerVersion.AutoDetect(appConn);
        }
        catch
        {
            serverVersion = ServerVersion.Parse("8.0.0-mysql");
        }
    }

    opt.UseMySql(appConn, serverVersion);
});

// Rate limit
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISqlRunnerService, SqlRunnerService>();

// JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();

if (jwt is null ||
    string.IsNullOrWhiteSpace(jwt.Issuer) ||
    string.IsNullOrWhiteSpace(jwt.Audience) ||
    string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    throw new InvalidOperationException("Hiányos Jwt config.");
}

if (jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt SigningKey túl rövid.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
            NameClaimType = ClaimTypes.Email,
            RoleClaimType = ClaimTypes.Role
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthController.AuthCookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("FrontendCors");

app.UseRateLimiter();

// CSRF middleware
app.Use(async (context, next) =>
{
    if (HttpMethods.IsOptions(context.Request.Method))
    {
        await next();
        return;
    }

    var method = context.Request.Method;

    var isUnsafe =
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);

    var path = context.Request.Path.Value ?? "";

    var skip =
        path.StartsWith("/api/auth/login") ||
        path.StartsWith("/api/auth/register") ||
        path.StartsWith("/api/auth/logout") ||
        path.StartsWith("/api/auth/me") ||
        path.StartsWith("/swagger");

    if (isUnsafe && !skip)
    {
        var csrfCookie = context.Request.Cookies[AuthController.CsrfCookieName];
        var csrfHeader = context.Request.Headers["X-CSRF-TOKEN"].ToString();

        if (string.IsNullOrWhiteSpace(csrfHeader) || csrfCookie != csrfHeader)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("CSRF error");
            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireCors("FrontendCors");

app.Run();
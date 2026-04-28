using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SqlTrainer.Api.Auth;
using SqlTrainer.Api.Controllers;
using SqlTrainer.Api.Data;
using SqlTrainer.Api.Services;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5173",
                  "https://sqltraining.up.railway.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
    );
});

builder.Services.AddControllers()
  .AddJsonOptions(o =>
  {
      o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

builder.Services.AddEndpointsApiExplorer();

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

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RunnerOptions>(builder.Configuration.GetSection("Runner"));

var appConn = builder.Configuration.GetConnectionString("AppDb")!;
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

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
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

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("DevCors");
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var isUnsafeMethod = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
    var path = context.Request.Path.Value ?? string.Empty;
    var isAuthBootstrapEndpoint = path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/api/auth/me", StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);

    if (isUnsafeMethod && !isAuthBootstrapEndpoint)
    {
        var csrfCookie = context.Request.Cookies[AuthController.CsrfCookieName];
        var csrfHeader = context.Request.Headers["X-CSRF-TOKEN"].ToString();

        if (string.IsNullOrWhiteSpace(csrfCookie) || !string.Equals(csrfCookie, csrfHeader, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Hiányzó vagy hibás CSRF token.");
            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

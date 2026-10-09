using Limita.Api.Extensions;
using Limita.Api.Middleware;
using Limita.Api.Services;
using Limita.Application;
using Limita.Application.Common.Abstractions;
using Limita.Common;
using Limita.Development;
using Limita.Infrastructure;
using Limita.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration)
                .AddJwtAuthentication(builder.Configuration);


builder.Services
    .AddControllers(options =>
    {
        // FluentValidation is the single place for input rules, so MVC must not add its own "required" checks
        // for non-nullable properties. A missing field reaches the validators and gets one consistent error shape.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// The "Console" SMS provider prints one-time codes to the log, so it must never run outside Development.
if(!builder.Environment.IsDevelopment() &&
    string.Equals(builder.Configuration["Sms:Provider"], "Console", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
    "Sms:Provider 'Console' prints one-time codes to the log and is only allowed in Development.");
}

var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Public auth actions (login, OTP, reset): RateLimiting:AuthPermitLimit requests per minute (default 10) per IP address.
    // Behind a reverse proxy, configure forwarded headers so this sees the real client IP.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await DevelopmentDataSeeder.EnsureAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Secure by default: ApiControllerBase has [Authorize], so every action needs a valid token unless it is [AllowAnonymous].
app.MapControllers();

app.Run();






builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token here."
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

    options.OperationFilter<AllowAnonymousOperationFilter>();
});


var jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwt.SecretKey) || jwt.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "The JWT secret key must contain at least 32 characters.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });








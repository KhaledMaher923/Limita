using Limita.Api.Extensions;
using Limita.Api.Middleware;
using Limita.Application;
using Limita.Development;
using Limita.Infrastructure;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// The "Console" SMS provider prints one-time codes to the log, so it must never run outside Development.
if (!builder.Environment.IsDevelopment() &&
    string.Equals(builder.Configuration["Sms:Provider"], "Console", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Sms:Provider 'Console' prints one-time codes to the log and is only allowed in Development.");
}

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration); // the only place JWT bearer authentication is registered

builder.Services
    .AddControllers(options =>
    {
        // FluentValidation is the single place for input rules, so MVC must not add its own "required" checks
        // for non-nullable properties. A missing field reaches the validators and gets one consistent error shape.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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

// Required so the integration tests can reference the entry point.
public partial class Program { }

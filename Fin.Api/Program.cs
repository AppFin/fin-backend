using System.Net;
using System.Threading.RateLimiting;
using Fin.Application.Notifications.Extensions;
using Fin.Infrastructure.Constants;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Extensions;
using Fin.Infrastructure.Seeders.Extensions;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

var frontEndUrl = builder.Configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
var version = builder.Configuration.GetSection(AppConstants.VersionConfigKey).Get<string>();

builder.Services.AddRazorPages();

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddOpenApiDocument(config =>
    {
        config.Title = "FinApp API";
        config.Version = "v1";

        config.AddSecurity("Bearer", [], new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
    })
    .AddCors(options =>
    {
        options.AddPolicy("AllowAngularLocalhost",
            policy =>
            {
                policy.WithOrigins(frontEndUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    })
    .AddControllers();

// Second layer of abuse protection behind Cloudflare/the reverse proxy: throttles
// the unauthenticated auth/signup endpoints per client IP, since nothing else in
// the app limits how many accounts/login attempts a single visitor can hammer.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(version))
{
    var versionPathBase = version;
    if (!versionPathBase.StartsWith("/")) versionPathBase = $"/{versionPathBase}";
    app.UsePathBase(versionPathBase);
}


if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
    app.UseHangfireDashboard();
}

// Runs behind a reverse proxy that terminates TLS (e.g. the portfolio demo deploy),
// so Kestrel needs the forwarded headers to know the original request was HTTPS and
// who the real client is. The proxy reaches this container over the Oracle private
// network, not loopback, so the default (loopback-only) trusted-proxy list would
// silently ignore the headers - explicitly trust that private subnet instead.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 24));
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseRateLimiter();

app.UseCors("AllowAngularLocalhost");

app.UseNotifications();
app.UseFinMiddlewares();

await app.UseDbMigrations();
await app.UseSeeders();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHsts();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.Run();
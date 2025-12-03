using Fin.Application.Notifications.Extensions;
using Fin.Infrastructure.Constants;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Extensions;
using Fin.Infrastructure.Seeders.Extensions;
using Hangfire;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

var frontEndUrl = builder.Configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
var version = builder.Configuration.GetSection(AppConstants.VersionConfigKey).Get<string>();

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
    app.UseCors("AllowAngularLocalhost");
}

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

app.MapControllers();
app.Run();
using Fin.Application.Notifications.Extensions;
using Fin.Infrastructure.Constants;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Extensions;
using Fin.Infrastructure.Seeders.Extensions;
using Hangfire;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

var frontEndUrl = builder.Configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseHsts();
app.UseHttpsRedirection();

app.MapControllers();
app.Run();
using Fin.Application.Notifications.Extensions;
using Fin.Infrastructure.Extensions;
using Fin.Infrastructure.Notifications.Hubs;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddNotifications()
    .AddOpenApiDocument()
    .AddCors(options =>
    {
        options.AddPolicy("AllowAngularLocalhost",
            policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
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

app.UseAuthentication();
app.UseAuthorization();

app.UseHsts();
app.UseHttpsRedirection();

app.MapControllers();
app.Run();
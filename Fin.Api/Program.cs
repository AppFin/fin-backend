using Fin.Infrastructure;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications;
using Fin.Infrastructure.Errors;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddOpenApiDocument()
    .AddControllers();

builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseMiddleware<AmbientDataMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseHsts();
app.UseHttpsRedirection();

app.UseHangfireDashboard();

app.MapControllers();
app.Run();
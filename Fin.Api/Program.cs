using Fin.Application.AutoServices.Extensions;
using Fin.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddAutoSingletonServices()
    .AddAutoScopedServices()
    .AddAutoTransientServices()
    .AddOpenApiDocument()
    .AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.MapControllers();
app.Run();
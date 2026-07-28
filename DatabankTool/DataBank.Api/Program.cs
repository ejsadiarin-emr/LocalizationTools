using DataBank.Api.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DataBank API",
        Version = "v1",
        Description = "REST API for accessing and managing localization data extracted by the DataBank CLI tool."
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IDataBankService, FileDataBankService>();
builder.Services.AddSingleton<IExtractionService, ExtractionService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataBank API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowConfiguredOrigins");
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

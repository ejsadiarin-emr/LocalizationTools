using DataBank.Api.Endpoints;
using DataBank.Api.Repositories;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

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

var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "databank";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});
builder.Services.AddScoped<IDataBankRepository, MongoDataBankRepository>();

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

app.MapEntriesEndpoints();
app.MapMetadataEndpoints();
app.MapSessionsEndpoints();
app.MapExtractionEndpoints();
app.MapStatsEndpoints();
app.MapExportEndpoints();

app.MapHealthChecks("/health");

app.Run();

using DataBank.Api.Models;
using DataBank.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Endpoints;

public static class EntriesEndpoints
{
    public static void MapEntriesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/entries")
            .WithTags("Entries");

        group.MapGet("/", async (
            IDataBankRepository repository,
            [FromQuery] string? locale = null,
            [FromQuery] string? format = null,
            [FromQuery] string? key = null) =>
        {
            var entries = await repository.GetFilteredEntriesAsync(locale, format, key);
            return Results.Ok(entries);
        })
        .WithName("GetEntries")
        .WithDescription("Get all entries with optional locale, format, and key filters");

        group.MapGet("/count", async (
            IDataBankRepository repository,
            [FromQuery] string? locale = null) =>
        {
            var count = await repository.GetEntryCountAsync(locale);
            return Results.Ok(new { count });
        })
        .WithName("GetEntriesCount")
        .WithDescription("Get total entry count, optionally filtered by locale");

        group.MapGet("/{id}", async (string id, IDataBankRepository repository) =>
        {
            var entry = await repository.GetEntryByIdAsync(id);
            if (entry is null)
                return Results.NotFound(new { error = $"Entry with ID '{id}' not found." });
            return Results.Ok(entry);
        })
        .WithName("GetEntryById")
        .WithDescription("Get a single entry by ID");

        group.MapPost("/", async (DataBankEntryDocument entry, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByKeyAsync(entry.Key);
            if (existing is not null)
                return Results.Conflict(new { error = $"An entry with key '{entry.Key}' already exists." });

            var created = await repository.CreateEntryAsync(entry);
            return Results.Created($"/api/entries/{created.Id}", created);
        })
        .WithName("CreateEntry")
        .WithDescription("Create a new entry");

        group.MapPut("/{id}", async (string id, DataBankEntryDocument entry, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByIdAsync(id);
            if (existing is null)
                return Results.NotFound(new { error = $"Entry with ID '{id}' not found." });

            entry.Id = id;
            await repository.UpdateEntryAsync(id, entry);
            return Results.Ok(entry);
        })
        .WithName("UpdateEntry")
        .WithDescription("Update an existing entry");

        group.MapDelete("/{id}", async (string id, IDataBankRepository repository) =>
        {
            var deleted = await repository.DeleteEntryAsync(id);
            if (!deleted)
                return Results.NotFound(new { error = $"Entry with ID '{id}' not found." });
            return Results.NoContent();
        })
        .WithName("DeleteEntry")
        .WithDescription("Delete an entry");
    }
}

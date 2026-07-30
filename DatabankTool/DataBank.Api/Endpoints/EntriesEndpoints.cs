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

        group.MapGet("/by-key/{key}", async (string key, IDataBankRepository repository) =>
        {
            var entry = await repository.GetEntryByKeyAsync(key);
            if (entry is null)
                return Results.NotFound(new { error = $"Entry with key '{key}' not found." });
            return Results.Ok(entry);
        })
        .WithName("GetEntryByKey")
        .WithDescription("Get a single entry by key");

        group.MapPost("/", async (DataBankEntryDocument entry, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByKeyAsync(entry.Key);
            if (existing is not null)
                return Results.Conflict(new { error = $"An entry with key '{entry.Key}' already exists." });

            entry.Id = entry.Key;
            var created = await repository.CreateEntryAsync(entry);
            return Results.Created($"/api/entries/{created.Id}", created);
        })
        .WithName("CreateEntry")
        .WithDescription("Create a new entry");

        group.MapPut("/{key}", async (string key, DataBankEntryDocument entry, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByKeyAsync(key);
            if (existing is null)
                return Results.NotFound(new { error = $"Entry with key '{key}' not found." });

            entry.Id = key;
            entry.Key = key;
            await repository.UpdateEntryAsync(key, entry);
            return Results.Ok(entry);
        })
        .WithName("UpdateEntry")
        .WithDescription("Update an existing entry");

        group.MapPut("/{key}/locales/{locale}", async (
            string key, string locale, UpdateLocaleValueRequest request, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByKeyAsync(key);
            if (existing is null)
                return Results.NotFound(new { error = $"Entry with key '{key}' not found." });

            var updated = await repository.UpdateLocaleValueAsync(key, locale, request.Value);
            if (!updated)
                return Results.BadRequest(new { error = $"Failed to update locale '{locale}' for key '{key}'." });

            var entry = await repository.GetEntryByKeyAsync(key);
            return Results.Ok(entry);
        })
        .WithName("UpdateLocaleValue")
        .WithDescription("Update a specific locale value within an entry");

        group.MapPatch("/{key}/values", async (
            string key, BulkUpdateValuesRequest request, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByKeyAsync(key);
            if (existing is null)
                return Results.NotFound(new { error = $"Entry with key '{key}' not found." });

            // Update each locale value
            foreach (var val in request.Values)
            {
                await repository.UpdateLocaleValueAsync(key, val.Locale, val.Value);
            }

            var entry = await repository.GetEntryByKeyAsync(key);
            return Results.Ok(entry);
        })
        .WithName("BulkUpdateValues")
        .WithDescription("Bulk update multiple locale values for an entry");

        group.MapDelete("/{key}", async (string key, IDataBankRepository repository) =>
        {
            var existing = await repository.GetEntryByKeyAsync(key);
            if (existing is null)
                return Results.NotFound(new { error = $"Entry with key '{key}' not found." });

            var deleted = await repository.DeleteEntryAsync(key);
            if (!deleted)
                return Results.NotFound(new { error = $"Entry with key '{key}' not found." });
            return Results.NoContent();
        })
        .WithName("DeleteEntry")
        .WithDescription("Delete an entry");
    }
}

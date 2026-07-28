using DataBank.Api.Models;
using DataBank.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Endpoints;

public static class SessionsEndpoints
{
    public static void MapSessionsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Translation Sessions");

        group.MapGet("/", async (
            IDataBankRepository repository,
            [FromQuery] string? status = null) =>
        {
            var sessions = await repository.GetAllSessionsAsync(status);
            return Results.Ok(sessions);
        })
        .WithName("GetSessions")
        .WithDescription("Get all translation sessions, optionally filtered by status");

        group.MapGet("/{id}", async (string id, IDataBankRepository repository) =>
        {
            var session = await repository.GetSessionByIdAsync(id);
            if (session is null)
                return Results.NotFound(new { error = $"Session with ID '{id}' not found." });
            return Results.Ok(session);
        })
        .WithName("GetSessionById")
        .WithDescription("Get a single translation session by ID");

        group.MapPost("/", async (TranslationSessionDocument session, IDataBankRepository repository) =>
        {
            session.Status = TranslationSessionStatus.Pending;
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            var created = await repository.CreateSessionAsync(session);
            return Results.Created($"/api/sessions/{created.Id}", created);
        })
        .WithName("CreateSession")
        .WithDescription("Create a new translation session");

        group.MapPut("/{id}/status", async (
            string id,
            [FromBody] StatusUpdateRequest request,
            IDataBankRepository repository) =>
        {
            var session = await repository.GetSessionByIdAsync(id);
            if (session is null)
                return Results.NotFound(new { error = $"Session with ID '{id}' not found." });

            if (!TranslationSessionStatus.IsValidTransition(session.Status, request.Status))
            {
                return Results.BadRequest(new
                {
                    error = $"Invalid status transition from '{session.Status}' to '{request.Status}'. " +
                            $"Valid transitions: pending -> in-progress, in-progress -> completed"
                });
            }

            await repository.UpdateSessionStatusAsync(id, request.Status);
            return Results.Ok(new { id, status = request.Status });
        })
        .WithName("UpdateSessionStatus")
        .WithDescription("Update session status (pending -> in-progress -> completed)");

        group.MapPost("/{id}/entries", async (
            string id,
            [FromBody] AddEntriesRequest request,
            IDataBankRepository repository) =>
        {
            var session = await repository.GetSessionByIdAsync(id);
            if (session is null)
                return Results.NotFound(new { error = $"Session with ID '{id}' not found." });

            await repository.AddEntriesToSessionAsync(id, request.EntryIds);
            return Results.Ok(new { id, entryIds = request.EntryIds });
        })
        .WithName("AddEntriesToSession")
        .WithDescription("Add entry IDs to a translation session");

        group.MapDelete("/{id}", async (string id, IDataBankRepository repository) =>
        {
            var deleted = await repository.DeleteSessionAsync(id);
            if (!deleted)
                return Results.NotFound(new { error = $"Session with ID '{id}' not found." });
            return Results.NoContent();
        })
        .WithName("DeleteSession")
        .WithDescription("Delete a translation session");
    }
}

public class StatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AddEntriesRequest
{
    public List<string> EntryIds { get; set; } = [];
}

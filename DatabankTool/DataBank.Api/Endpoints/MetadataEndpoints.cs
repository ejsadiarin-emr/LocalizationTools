using DataBank.Api.Repositories;

namespace DataBank.Api.Endpoints;

public static class MetadataEndpoints
{
    public static void MapMetadataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/metadata")
            .WithTags("Metadata");

        group.MapGet("/", async (IDataBankRepository repository) =>
        {
            var metadata = await repository.GetMetadataAsync();
            if (metadata is null)
                return Results.NotFound(new { error = "No metadata found." });
            return Results.Ok(metadata);
        })
        .WithName("GetMetadata")
        .WithDescription("Get dataset metadata");
    }
}

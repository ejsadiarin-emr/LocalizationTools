using DataBank.Api.Models;
using DataBank.Api.Services;
using DataBank.Cli.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntriesController : ControllerBase
{
    private readonly IDataBankService _dataBankService;

    public EntriesController(IDataBankService dataBankService)
    {
        _dataBankService = dataBankService;
    }

    /// <summary>
    /// Gets all localization entries with optional filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<LocalizedStringEntry>), StatusCodes.Status200OK)]
    public IActionResult GetEntries(
        [FromQuery] string? locale = null,
        [FromQuery] string? format = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded. data-bank.json not found." });

        var entries = _dataBankService.GetAllEntries().AsEnumerable();

        if (!string.IsNullOrEmpty(locale))
            entries = entries.Where(e => e.Locale == locale);

        if (!string.IsNullOrEmpty(format))
            entries = entries.Where(e => e.Source.Format == format);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<TranslationStatus>(status, true, out var translationStatus))
                entries = entries.Where(e => e.Metadata.TranslationStatus == translationStatus);
        }

        var totalCount = entries.Count();
        var pagedEntries = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-PageSize", pageSize.ToString());

        return Ok(new PaginatedResult<LocalizedStringEntry>
        {
            Items = pagedEntries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Gets a single localization entry by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LocalizedStringEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetEntry(string id)
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded." });

        var entry = _dataBankService.GetById(id);
        if (entry is null)
            return NotFound(new { error = $"Entry with ID '{id}' not found." });

        return Ok(entry);
    }

    /// <summary>
    /// Creates a new localization entry.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocalizedStringEntry), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateEntry([FromBody] CreateEntryRequest request)
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded." });

        if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.Locale))
            return BadRequest(new { error = "Key and Locale are required." });

        var entry = new LocalizedStringEntry
        {
            Id = request.Id ?? $"{request.Source?.Format ?? "unknown"}::custom::{request.Key}",
            Key = request.Key,
            Value = request.Value ?? string.Empty,
            Locale = request.Locale,
            Source = request.Source ?? new SourceInfo { Format = "unknown", File = "api", Path = "api" },
            Metadata = request.Metadata ?? new EntryMetadata()
        };

        var created = _dataBankService.AddEntry(entry);
        return CreatedAtAction(nameof(GetEntry), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing localization entry.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LocalizedStringEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult UpdateEntry(string id, [FromBody] CreateEntryRequest request)
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded." });

        if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.Locale))
            return BadRequest(new { error = "Key and Locale are required." });

        var entry = new LocalizedStringEntry
        {
            Id = id,
            Key = request.Key,
            Value = request.Value ?? string.Empty,
            Locale = request.Locale,
            Source = request.Source ?? new SourceInfo { Format = "unknown", File = "api", Path = "api" },
            Metadata = request.Metadata ?? new EntryMetadata()
        };

        if (!_dataBankService.UpdateEntry(id, entry))
            return NotFound(new { error = $"Entry with ID '{id}' not found." });

        return Ok(entry);
    }

    /// <summary>
    /// Deletes a localization entry.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteEntry(string id)
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded." });

        if (!_dataBankService.DeleteEntry(id))
            return NotFound(new { error = $"Entry with ID '{id}' not found." });

        return NoContent();
    }
}

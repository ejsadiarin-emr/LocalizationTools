using DataBank.Api.Models;
using DataBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExtractController : ControllerBase
{
    private readonly IExtractionService _extractionService;

    public ExtractController(IExtractionService extractionService)
    {
        _extractionService = extractionService;
    }

    /// <summary>
    /// Triggers a file parsing and data extraction job.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult StartExtraction([FromBody] ExtractRequest request)
    {
        if (string.IsNullOrEmpty(request.SourceDirectory))
            return BadRequest(new { error = "SourceDirectory is required." });

        var jobId = _extractionService.StartExtraction(request.SourceDirectory, request.FilePatterns);
        return Accepted(new { jobId, message = "Extraction job started." });
    }

    /// <summary>
    /// Gets the status of an extraction job.
    /// </summary>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(ExtractionJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetJobStatus(string jobId)
    {
        var job = _extractionService.GetJobStatus(jobId);
        if (job is null)
            return NotFound(new { error = $"Job '{jobId}' not found." });

        return Ok(job);
    }
}

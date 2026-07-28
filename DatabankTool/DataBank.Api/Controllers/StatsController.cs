using DataBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly IDataBankService _dataBankService;

    public StatsController(IStatisticsService statisticsService, IDataBankService dataBankService)
    {
        _statisticsService = statisticsService;
        _dataBankService = dataBankService;
    }

    /// <summary>
    /// Gets comprehensive localization statistics.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StatisticsResult), StatusCodes.Status200OK)]
    public IActionResult GetStats()
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded." });

        return Ok(_statisticsService.GetStatistics());
    }

    /// <summary>
    /// Gets coverage summary information.
    /// </summary>
    [HttpGet("coverage")]
    [ProducesResponseType(typeof(Cli.Models.CoverageReport), StatusCodes.Status200OK)]
    public IActionResult GetCoverage(
        [FromQuery] string? locale = null,
        [FromQuery] string? format = null)
    {
        if (!_dataBankService.IsDataLoaded)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data not loaded." });

        return Ok(_statisticsService.GetCoverage(locale, format));
    }
}

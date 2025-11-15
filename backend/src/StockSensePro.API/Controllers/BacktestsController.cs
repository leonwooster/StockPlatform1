using Microsoft.AspNetCore.Mvc;
using StockSensePro.API.Models;
using StockSensePro.Application.Interfaces;
using StockSensePro.Application.Models;
using System.Linq;
using System;

namespace StockSensePro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BacktestsController : ControllerBase
    {
        private readonly IBacktestService _backtestService;

        public BacktestsController(IBacktestService backtestService)
        {
            _backtestService = backtestService;
        }

        [HttpPost("run")]
        public async Task<ActionResult<BacktestResult>> RunBacktest([FromBody] RunBacktestRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (request.StartDate == default || request.EndDate == default)
            {
                return BadRequest("StartDate and EndDate are required.");
            }

            if (request.StartDate >= request.EndDate)
            {
                return BadRequest("StartDate must be earlier than EndDate.");
            }

            var result = await _backtestService.RunBacktestAsync(request.ToBacktestRequest(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("{symbol}/summary")]
        public async Task<ActionResult<BacktestSummary>> GetSummary(string symbol, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            var summary = await _backtestService.GetPerformanceSummaryAsync(symbol, cancellationToken);
            return Ok(summary);
        }

        [HttpGet("{symbol}/recent")]
        public async Task<ActionResult<IEnumerable<SignalPerformanceDto>>> GetRecent(string symbol, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            if (take <= 0)
            {
                take = 20;
            }

            var recent = await _backtestService.GetRecentPerformancesAsync(symbol, take, cancellationToken);
            return Ok(recent.Select(p => p.ToDto()));
        }

        [HttpGet("{symbol}/dashboard")]
        public async Task<ActionResult<BacktestPerformanceResponse>> GetDashboard(string symbol, [FromQuery] int recent = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            var summaryTask = _backtestService.GetPerformanceSummaryAsync(symbol, cancellationToken);
            var recentTask = _backtestService.GetRecentPerformancesAsync(symbol, recent, cancellationToken);

            await Task.WhenAll(summaryTask, recentTask);

            var response = new BacktestPerformanceResponse
            {
                Summary = summaryTask.Result,
                RecentPerformances = recentTask.Result.Select(p => p.ToDto())
            };

            return Ok(response);
        }

        [HttpGet("{symbol}/equity-curve")]
        public async Task<ActionResult<IReadOnlyList<EquityCurvePoint>>> GetEquityCurve(
            string symbol,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool compounded = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            var curve = await _backtestService.GetEquityCurveAsync(symbol, startDate, endDate, compounded, cancellationToken);
            return Ok(curve);
        }

        [HttpGet("{symbol}/equity-curve/daily")]
        public async Task<ActionResult<IReadOnlyList<EquityCurvePoint>>> GetEquityCurveDaily(
            string symbol,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool compounded = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest("Symbol is required.");
            }

            if (startDate == default || endDate == default || startDate > endDate)
            {
                return BadRequest("Valid startDate and endDate are required, and startDate must be before or equal to endDate.");
            }

            var curve = await _backtestService.GetEquityCurveDailyAsync(symbol, startDate, endDate, compounded, cancellationToken);
            return Ok(curve);
        }
    }
}

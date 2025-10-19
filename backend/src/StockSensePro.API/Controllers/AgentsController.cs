using Microsoft.AspNetCore.Mvc;
using StockSensePro.AI.Services;

namespace StockSensePro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly IAgentService _agentService;

        public AgentsController(IAgentService agentService)
        {
            _agentService = agentService;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<AgentAnalysisResult>> AnalyzeStock([FromBody] AnalyzeRequest request)
        {
            var enabledAgents = request.EnabledAgents
                .Select(a => Enum.Parse<AgentType>(a))
                .ToList();

            var result = await _agentService.AnalyzeStockAsync(
                request.Symbol,
                enabledAgents,
                request.IncludeDebate,
                request.IncludeRiskAssessment);

            return Ok(result);
        }
    }

    public class AnalyzeRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public List<string> EnabledAgents { get; set; } = new();
        public bool IncludeDebate { get; set; } = true;
        public bool IncludeRiskAssessment { get; set; } = true;
    }
}

using System.Text.Json;

namespace StockSensePro.AI.Services
{
    public interface IAgentService
    {
        Task<AgentAnalysisResult> AnalyzeStockAsync(string symbol, List<AgentType> enabledAgents, bool includeDebate, bool includeRiskAssessment);
    }

    public class AgentAnalysisResult
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TradingSignal Signal { get; set; } = new();
        public List<AgentAnalysis> Analyses { get; set; } = new();
        public AgentDebate Debate { get; set; } = new();
        public RiskAssessment RiskAssessment { get; set; } = new();
    }

    public class AgentAnalysis
    {
        public AgentType AgentType { get; set; }
        public string Analysis { get; set; } = string.Empty;
        public Dictionary<string, object> Metrics { get; set; } = new();
        public int ConfidenceScore { get; set; }
        public string Rationale { get; set; } = string.Empty;
    }

    public class TradingSignal
    {
        public SignalType Type { get; set; }
        public int Confidence { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal StopLoss { get; set; }
        public string Rationale { get; set; } = string.Empty;
    }

    public class AgentDebate
    {
        public List<DebateArgument> BullishArguments { get; set; } = new();
        public List<DebateArgument> BearishArguments { get; set; } = new();
        public string Consensus { get; set; } = string.Empty;
        public int BullishScore { get; set; }
        public int BearishScore { get; set; }
    }

    public class DebateArgument
    {
        public string Point { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public int Weight { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class RiskAssessment
    {
        public int RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public decimal VolatilityScore { get; set; }
        public decimal BetaValue { get; set; }
        public decimal RecommendedPositionSize { get; set; }
        public decimal SuggestedStopLoss { get; set; }
        public decimal SuggestedTakeProfit { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public string MarketRegime { get; set; } = string.Empty;
    }

    public enum AgentType
    {
        FundamentalAnalyst,
        SentimentAnalyst,
        NewsAnalyst,
        TechnicalAnalyst,
        BullishResearcher,
        BearishResearcher,
        Trader,
        RiskManager
    }

    public enum SignalType
    {
        StrongBuy,
        Buy,
        Hold,
        Sell,
        StrongSell
    }

    public enum RiskLevel
    {
        VeryLow,
        Low,
        Moderate,
        High,
        VeryHigh
    }
}

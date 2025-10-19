using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using StockSensePro.AI.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StockSensePro.API.Filters
{
    public class AgentAnalysisResultSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != typeof(AgentAnalysisResult))
            {
                return;
            }

            schema.Example = new OpenApiObject
            {
                [nameof(AgentAnalysisResult.Symbol)] = new OpenApiString("AAPL"),
                [nameof(AgentAnalysisResult.Timestamp)] = new OpenApiString("2025-10-18T16:15:00Z"),
                [nameof(AgentAnalysisResult.Signal)] = new OpenApiObject
                {
                    [nameof(TradingSignal.Type)] = new OpenApiString(SignalType.Buy.ToString()),
                    [nameof(TradingSignal.Confidence)] = new OpenApiInteger(78),
                    [nameof(TradingSignal.TargetPrice)] = new OpenApiDouble(192.75),
                    [nameof(TradingSignal.StopLoss)] = new OpenApiDouble(174.50),
                    [nameof(TradingSignal.Rationale)] = new OpenApiString("Strong revenue growth, bullish technical breakout, and positive sentiment support upside momentum.")
                },
                [nameof(AgentAnalysisResult.Analyses)] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        [nameof(AgentAnalysis.AgentType)] = new OpenApiString(AgentType.FundamentalAnalyst.ToString()),
                        [nameof(AgentAnalysis.Analysis)] = new OpenApiString("Revenue growth of 8.4% YoY with EPS beating consensus by 6%. Gross margin expansion to 44.5% indicates resilient pricing power."),
                        [nameof(AgentAnalysis.Metrics)] = new OpenApiObject
                        {
                            ["RevenueGrowthYoY"] = new OpenApiDouble(8.4),
                            ["EpsBeatPercentage"] = new OpenApiDouble(6.1),
                            ["GrossMargin"] = new OpenApiDouble(44.5)
                        },
                        [nameof(AgentAnalysis.ConfidenceScore)] = new OpenApiInteger(86),
                        [nameof(AgentAnalysis.Rationale)] = new OpenApiString("Earnings momentum remains strong with diversified revenue streams reducing downside risk.")
                    },
                    new OpenApiObject
                    {
                        [nameof(AgentAnalysis.AgentType)] = new OpenApiString(AgentType.TechnicalAnalyst.ToString()),
                        [nameof(AgentAnalysis.Analysis)] = new OpenApiString("Price broke above the 50-day moving average with 1.6x average volume. RSI at 62 indicates bullish momentum without overbought conditions."),
                        [nameof(AgentAnalysis.Metrics)] = new OpenApiObject
                        {
                            ["RSI"] = new OpenApiDouble(62.1),
                            ["VolumeMultiple"] = new OpenApiDouble(1.6),
                            ["50DMA"] = new OpenApiDouble(182.34)
                        },
                        [nameof(AgentAnalysis.ConfidenceScore)] = new OpenApiInteger(74),
                        [nameof(AgentAnalysis.Rationale)] = new OpenApiString("Momentum indicators and volume profile confirm a sustainable breakout pattern.")
                    },
                    new OpenApiObject
                    {
                        [nameof(AgentAnalysis.AgentType)] = new OpenApiString(AgentType.SentimentAnalyst.ToString()),
                        [nameof(AgentAnalysis.Analysis)] = new OpenApiString("Social sentiment is 71% positive with elevated discussion volume across Reddit and StockTwits following product launch news."),
                        [nameof(AgentAnalysis.Metrics)] = new OpenApiObject
                        {
                            ["BullishMentions"] = new OpenApiInteger(7120),
                            ["BearishMentions"] = new OpenApiInteger(2850),
                            ["DiscussionVolumeChange"] = new OpenApiDouble(24.5)
                        },
                        [nameof(AgentAnalysis.ConfidenceScore)] = new OpenApiInteger(68),
                        [nameof(AgentAnalysis.Rationale)] = new OpenApiString("Consistently positive sentiment trend with increasing participation across multiple channels.")
                    }
                },
                [nameof(AgentAnalysisResult.Debate)] = new OpenApiObject
                {
                    [nameof(AgentDebate.BullishArguments)] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            [nameof(DebateArgument.Point)] = new OpenApiString("Services segment accelerating to record margins"),
                            [nameof(DebateArgument.Evidence)] = new OpenApiString("Services revenue up 12% YoY with subscription ARPU at an all-time high."),
                            [nameof(DebateArgument.Weight)] = new OpenApiInteger(82),
                            [nameof(DebateArgument.Source)] = new OpenApiString("Q4 FY25 Earnings Call")
                        },
                        new OpenApiObject
                        {
                            [nameof(DebateArgument.Point)] = new OpenApiString("AI-powered product refresh driving upgrade cycle"),
                            [nameof(DebateArgument.Evidence)] = new OpenApiString("Pre-orders for the new AI-enabled devices tracking 18% above last year's launch.")
                            ,
                            [nameof(DebateArgument.Weight)] = new OpenApiInteger(77),
                            [nameof(DebateArgument.Source)] = new OpenApiString("Canalys Market Pulse")
                        }
                    },
                    [nameof(AgentDebate.BearishArguments)] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            [nameof(DebateArgument.Point)] = new OpenApiString("Valuation premium remains elevated"),
                            [nameof(DebateArgument.Evidence)] = new OpenApiString("Forward P/E at 27.8x vs. sector median of 21.4x."),
                            [nameof(DebateArgument.Weight)] = new OpenApiInteger(64),
                            [nameof(DebateArgument.Source)] = new OpenApiString("Bloomberg Market Data")
                        },
                        new OpenApiObject
                        {
                            [nameof(DebateArgument.Point)] = new OpenApiString("Regulatory scrutiny on App Store practices"),
                            [nameof(DebateArgument.Evidence)] = new OpenApiString("EU DMA compliance could pressure high-margin services revenue in 2026."),
                            [nameof(DebateArgument.Weight)] = new OpenApiInteger(58),
                            [nameof(DebateArgument.Source)] = new OpenApiString("European Commission Briefing")
                        }
                    },
                    [nameof(AgentDebate.Consensus)] = new OpenApiString("Moderately bullish: upside catalysts outweigh valuation and regulatory risks in the next 6 months."),
                    [nameof(AgentDebate.BullishScore)] = new OpenApiInteger(81),
                    [nameof(AgentDebate.BearishScore)] = new OpenApiInteger(61)
                },
                [nameof(AgentAnalysisResult.RiskAssessment)] = new OpenApiObject
                {
                    [nameof(RiskAssessment.RiskScore)] = new OpenApiInteger(42),
                    [nameof(RiskAssessment.RiskLevel)] = new OpenApiString(RiskLevel.Moderate.ToString()),
                    [nameof(RiskAssessment.VolatilityScore)] = new OpenApiDouble(0.26),
                    [nameof(RiskAssessment.BetaValue)] = new OpenApiDouble(1.12),
                    [nameof(RiskAssessment.RecommendedPositionSize)] = new OpenApiDouble(5.5),
                    [nameof(RiskAssessment.SuggestedStopLoss)] = new OpenApiDouble(174.50),
                    [nameof(RiskAssessment.SuggestedTakeProfit)] = new OpenApiDouble(198.00),
                    [nameof(RiskAssessment.RiskFactors)] = new OpenApiArray
                    {
                        new OpenApiString("Macro-driven multiple compression"),
                        new OpenApiString("FX headwinds impacting international sales")
                    },
                    [nameof(RiskAssessment.MarketRegime)] = new OpenApiString("Bullish momentum with low breadth")
                }
            };
        }
    }
}

<template>
  <div class="space-y-10">
    <section class="rounded-3xl border border-white/5 bg-slate-900/60 p-8 shadow-soft">
      <div class="flex flex-col gap-8 lg:flex-row lg:items-center lg:justify-between">
        <div class="space-y-4">
          <h1 class="text-3xl font-semibold tracking-tight text-white">AI Trading Agents</h1>
          <p class="max-w-2xl text-sm text-slate-300">
            Coordinate a swarm of specialist agents—fundamental, technical, sentiment, and risk—to surface institutional-quality trade setups in seconds.
          </p>
          <div class="flex flex-wrap items-center gap-2 text-xs text-slate-300">
            <span class="rounded-full border border-primary-400/30 bg-primary-500/10 px-3 py-1 text-primary-200">Agents active: {{ activeAgentCount }}</span>
            <span class="rounded-full border border-white/10 bg-white/5 px-3 py-1">Playbook: Multi-factor blend</span>
            <span v-if="analysisResult" class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-3 py-1 text-emerald-200">Last signal · {{ analysisResult.signal.type }}</span>
          </div>
        </div>
        <div class="w-full max-w-xl space-y-4">
          <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
            <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Symbol</label>
            <div class="mt-2 flex flex-col gap-3 sm:flex-row">
              <input
                v-model="selectedSymbol"
                type="text"
                class="flex-1 rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-sm tracking-wide text-white placeholder:text-slate-500 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40"
                placeholder="e.g. AAPL"
                @keyup.enter="analyzeStock"
                @blur="loadBacktestDashboard(selectedSymbol)"
              >
              <button
                @click="analyzeStock"
                :disabled="analyzing"
                class="rounded-xl bg-primary-500 px-5 py-3 text-sm font-semibold text-white shadow-soft transition hover:bg-primary-400 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {{ analyzing ? 'Analyzing…' : 'Analyze' }}
              </button>
            </div>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-5">
            <p class="text-xs uppercase tracking-[0.3em] text-slate-500">Active agents</p>
            <div class="mt-3 flex flex-wrap gap-2">
              <button
                v-for="agent in agentDefinitions"
                :key="agent.key"
                @click="toggleAgent(agent.key)"
                class="rounded-full border px-4 py-2 text-xs font-medium transition"
                :class="enabledAgents[agent.key]
                  ? 'border-primary-400/40 bg-primary-500/15 text-primary-100'
                  : 'border-white/10 bg-white/5 text-slate-300 hover:border-primary-300/40 hover:text-primary-100'"
              >
                {{ agent.label }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section v-if="analysisResult" class="space-y-6">
      <div class="grid gap-6 lg:grid-cols-[2fr_1fr]">
        <article class="space-y-6">
          <div class="rounded-3xl border border-emerald-400/30 bg-emerald-500/10 p-6 text-sm text-emerald-100">
            <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p class="text-xs uppercase tracking-[0.3em] text-emerald-200">Composite trading signal</p>
                <h2 class="mt-2 text-3xl font-semibold text-white">{{ analysisResult.signal.type }}</h2>
                <p class="mt-2 max-w-xl text-xs text-emerald-50/80">{{ analysisResult.signal.rationale }}</p>
              </div>
              <div class="flex w-full max-w-sm flex-col gap-3 rounded-2xl border border-emerald-400/30 bg-emerald-500/10 p-4">
                <div class="flex items-center justify-between text-xs font-medium">
                  <span>Confidence</span>
                  <span>{{ analysisResult.signal.confidence }}%</span>
                </div>
                <div class="h-2 w-full rounded-full bg-emerald-500/20">
                  <div
                    class="h-2 rounded-full"
                    :class="confidenceClass(analysisResult.signal.confidence)"
                    :style="`width: ${analysisResult.signal.confidence}%`"
                  ></div>
                </div>
                <div class="grid grid-cols-2 gap-3 text-xs">
                  <div class="rounded-xl border border-white/20 bg-white/10 p-3 text-center">
                    <p class="text-slate-200">Target</p>
                    <p class="mt-1 text-sm font-semibold text-white">${{ analysisResult.signal.targetPrice }}</p>
                  </div>
                  <div class="rounded-xl border border-white/20 bg-white/10 p-3 text-center">
                    <p class="text-slate-200">Stop loss</p>
                    <p class="mt-1 text-sm font-semibold text-white">${{ analysisResult.signal.stopLoss }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
            <div class="flex items-center justify-between">
              <div>
                <h2 class="text-sm font-semibold text-white">Agent briefs</h2>
                <p class="text-xs text-slate-400">Breakdown by specialist perspective</p>
              </div>
              <span class="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs text-slate-300">{{ analysisResult.analyses.length }} reports</span>
            </div>
            <div class="mt-6 space-y-4">
              <div
                v-for="analysis in analysisResult.analyses"
                :key="analysis.agentType"
                class="rounded-2xl border border-white/10 bg-white/5 p-5 text-sm text-slate-200"
              >
                <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                  <div class="text-white">{{ getAgentName(analysis.agentType) }}</div>
                  <span
                    class="rounded-full px-3 py-1 text-xs"
                    :class="confidenceBadgeClass(analysis.confidenceScore)"
                  >
                    Confidence {{ analysis.confidenceScore }}%
                  </span>
                </div>
                <p class="mt-3 text-slate-300">{{ analysis.analysis }}</p>
              </div>
            </div>
          </div>

          <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
            <div class="grid gap-6 lg:grid-cols-2">
              <div class="space-y-3 text-sm text-slate-200">
                <h3 class="text-xs uppercase tracking-[0.3em] text-slate-500">Risk assessment</h3>
                <div>
                  <div class="flex items-center justify-between text-xs">
                    <span>Risk score</span>
                    <span>{{ analysisResult.riskAssessment.riskScore }}/100</span>
                  </div>
                  <div class="mt-2 h-2 w-full rounded-full bg-white/10">
                    <div
                      class="h-2 rounded-full"
                      :class="riskClass(analysisResult.riskAssessment.riskScore)"
                      :style="`width: ${analysisResult.riskAssessment.riskScore}%`"
                    ></div>
                  </div>
                  <p class="mt-2 text-xs text-slate-400">{{ analysisResult.riskAssessment.riskLevel }} risk</p>
                </div>
                <div class="grid grid-cols-2 gap-3 text-xs">
                  <div class="rounded-xl border border-white/10 bg-white/5 p-3">
                    <p class="text-slate-300">Position size</p>
                    <p class="mt-1 text-sm font-semibold text-white">{{ analysisResult.riskAssessment.recommendedPositionSize }}%</p>
                  </div>
                  <div class="rounded-xl border border-white/10 bg-white/5 p-3">
                    <p class="text-slate-300">Take profit</p>
                    <p class="mt-1 text-sm font-semibold text-white">${{ analysisResult.riskAssessment.suggestedTakeProfit }}</p>
                  </div>
                  <div class="rounded-xl border border-white/10 bg-white/5 p-3">
                    <p class="text-slate-300">Stop loss</p>
                    <p class="mt-1 text-sm font-semibold text-white">${{ analysisResult.riskAssessment.suggestedStopLoss }}</p>
                  </div>
                </div>
              </div>
              <div class="rounded-2xl border border-white/10 bg-white/5 p-5 text-xs text-slate-200">
                <h4 class="text-slate-300">Key risk factors</h4>
                <ul class="mt-3 space-y-2">
                  <li
                    v-for="factor in analysisResult.riskAssessment.riskFactors"
                    :key="factor"
                    class="flex items-center gap-2"
                  >
                    <span class="inline-block h-2 w-2 rounded-full bg-rose-300"></span>
                    <span>{{ factor }}</span>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </article>

        <aside class="space-y-6">
          <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
            <h2 class="text-sm font-semibold text-white">Agent debate</h2>
            <div class="mt-4 space-y-4 text-sm text-slate-200">
              <div>
                <p class="text-xs uppercase tracking-[0.3em] text-emerald-200">Bullish</p>
                <ul class="mt-2 space-y-2">
                  <li
                    v-for="(arg, index) in analysisResult.debate.bullishArguments"
                    :key="`bull-${index}`"
                    class="rounded-2xl border border-emerald-400/20 bg-emerald-500/10 px-4 py-3"
                  >
                    <p class="font-semibold text-white">{{ arg.point }}</p>
                    <p class="text-xs text-emerald-100/80">{{ arg.evidence }}</p>
                  </li>
                </ul>
              </div>
              <div>
                <p class="text-xs uppercase tracking-[0.3em] text-rose-300">Bearish</p>
                <ul class="mt-2 space-y-2">
                  <li
                    v-for="(arg, index) in analysisResult.debate.bearishArguments"
                    :key="`bear-${index}`"
                    class="rounded-2xl border border-rose-400/20 bg-rose-500/10 px-4 py-3"
                  >
                    <p class="font-semibold text-white">{{ arg.point }}</p>
                    <p class="text-xs text-rose-100/80">{{ arg.evidence }}</p>
                  </li>
                </ul>
              </div>
              <div class="rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-xs text-slate-200">
                <p class="text-slate-300">Consensus</p>
                <p class="mt-1 text-white">{{ analysisResult.debate.consensus }}</p>
              </div>
            </div>
          </div>

          <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
            <h2 class="text-sm font-semibold text-white">Signal log</h2>
            <div class="mt-4 space-y-3 text-xs text-slate-300">
              <div class="flex items-center justify-between rounded-2xl border border-white/10 bg-white/5 px-3 py-2">
                <span>Timestamp</span>
                <span class="text-white">{{ new Date(analysisResult.timestamp).toLocaleString() }}</span>
              </div>
              <div class="flex items-center justify-between rounded-2xl border border-white/10 bg-white/5 px-3 py-2">
                <span>Symbol</span>
                <span class="text-white">{{ analysisResult.symbol }}</span>
              </div>
              <div class="flex items-center justify-between rounded-2xl border border-white/10 bg-white/5 px-3 py-2">
                <span>Enabled agents</span>
                <span class="text-white">{{ activeAgentCount }}</span>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </section>

    <section v-else class="rounded-3xl border border-dashed border-white/10 bg-slate-900/40 p-12 text-center text-sm text-slate-300">
      <div class="mx-auto flex h-24 w-24 items-center justify-center rounded-full border border-white/20 bg-white/5">
        <svg class="h-10 w-10 text-primary-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 7h16M4 12h16M4 17h16" />
        </svg>
      </div>
      <h2 class="mt-6 text-xl font-semibold text-white">Request a signal</h2>
      <p class="mt-2 text-slate-400">Provide a ticker symbol and enable the agents you want to consult. We’ll synthesize their debate into a single actionable recommendation.</p>
    </section>

    <section class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
      <h2 class="text-sm font-semibold text-white">Desk performance snapshot</h2>
      <div class="mt-6 grid gap-4 md:grid-cols-4">
        <div class="rounded-2xl border border-white/10 bg-white/5 p-5 text-center">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-400">Signal accuracy</p>
          <p class="mt-3 text-2xl font-semibold text-white">72%</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-5 text-center">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-400">Average return</p>
          <p class="mt-3 text-2xl font-semibold text-emerald-200">12.4%</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-5 text-center">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-400">Win rate</p>
          <p class="mt-3 text-2xl font-semibold text-primary-200">68%</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-5 text-center">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-400">Signals generated</p>
          <p class="mt-3 text-2xl font-semibold text-white">42</p>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
import { backtestService } from '../services/api'

export default {
  name: 'AIAgentsView',
  data() {
    return {
      selectedSymbol: 'AAPL',
      analyzing: false,
      enabledAgents: {
        fundamental: true,
        technical: true,
        sentiment: true
      },
      agentDefinitions: [
        { key: 'fundamental', label: 'Fundamental' },
        { key: 'technical', label: 'Technical' },
        { key: 'sentiment', label: 'Sentiment' }
      ],
      analysisResult: null,
      backtestLoading: false,
      backtestRunning: false,
      backtestError: '',
      backtestSummary: null,
      recentPerformances: [],
      recentLimit: 10,
      backtestParams: {
        lookbackDays: 90,
        holdingPeriodDays: 5,
        stopLossPercent: null,
        takeProfitPercent: null
      }
    }
  },
  computed: {
    activeAgentCount() {
      return Object.values(this.enabledAgents).filter(Boolean).length
    },
    hasBacktestData() {
      return this.backtestSummary && this.backtestSummary.evaluatedSignals > 0
    }
  },
  mounted() {
    this.loadBacktestDashboard(this.selectedSymbol)
  },
  methods: {
    async loadBacktestDashboard(symbol) {
      const normalizedSymbol = (symbol || '').trim().toUpperCase()

      if (!normalizedSymbol) {
        this.backtestSummary = null
        this.recentPerformances = []
        return
      }

      try {
        this.backtestLoading = true
        this.backtestError = ''
        const take = Math.min(50, Math.max(5, Number(this.recentLimit || 0)))
        this.recentLimit = take
        const dashboard = await backtestService.getDashboard(normalizedSymbol, take)
        this.backtestSummary = dashboard?.summary ?? null
        this.recentPerformances = dashboard?.recentPerformances ?? []
      } catch (error) {
        console.error('Failed to load backtest dashboard', error)
        this.backtestError = 'Unable to load backtest metrics. Please try again.'
      } finally {
        this.backtestLoading = false
      }
    },

    async runBacktest() {
      const normalizedSymbol = (this.selectedSymbol || '').trim().toUpperCase()
      if (!normalizedSymbol) {
        this.backtestError = 'Symbol is required to run a backtest.'
        return
      }

      try {
        this.backtestRunning = true
        this.backtestError = ''

        const endDate = new Date()
        const startDate = new Date()
        const lookback = Math.max(5, Number(this.backtestParams.lookbackDays || 0))
        startDate.setDate(endDate.getDate() - lookback)

        const payload = {
          symbol: normalizedSymbol,
          startDate: startDate.toISOString(),
          endDate: endDate.toISOString(),
          holdingPeriodDays: Math.max(1, Number(this.backtestParams.holdingPeriodDays || 1)),
          stopLossPercent: this.parseNullableNumber(this.backtestParams.stopLossPercent),
          takeProfitPercent: this.parseNullableNumber(this.backtestParams.takeProfitPercent)
        }

        await backtestService.runBacktest(payload)
        await this.loadBacktestDashboard(normalizedSymbol)
      } catch (error) {
        console.error('Failed to run backtest', error)
        this.backtestError = error?.message || 'Backtest request failed.'
      } finally {
        this.backtestRunning = false
      }
    },

    onRecentLimitChange() {
      const sanitized = Math.min(50, Math.max(5, Number(this.recentLimit || 0)))
      if (sanitized !== this.recentLimit) {
        this.recentLimit = sanitized
      }
      this.loadBacktestDashboard(this.selectedSymbol)
    },

    parseNullableNumber(value) {
      if (value === null || value === undefined || value === '') {
        return null
      }
      const numeric = Number(value)
      return Number.isFinite(numeric) ? numeric : null
    },

    async analyzeStock() {
      if (!this.selectedSymbol || this.analyzing) return

      const normalizedSymbol = this.selectedSymbol.trim().toUpperCase()
      this.selectedSymbol = normalizedSymbol
      this.loadBacktestDashboard(normalizedSymbol)

      this.analyzing = true

      try {
        await new Promise(resolve => setTimeout(resolve, 1500))

        this.analysisResult = {
          symbol: normalizedSymbol,
          timestamp: new Date().toISOString(),
          signal: {
            type: 'Buy',
            confidence: 78,
            targetPrice: 185.50,
            stopLoss: 168.20,
            rationale: 'Strong fundamentals combined with positive technical indicators and sentiment suggest upward momentum.'
          },
          analyses: [
            {
              agentType: 'FundamentalAnalyst',
              analysis: 'Company shows strong revenue growth of 8.2% and healthy profit margins of 25.3%. Debt-to-equity ratio is favorable at 0.45.',
              confidenceScore: 85
            },
            {
              agentType: 'TechnicalAnalyst',
              analysis: 'Price is breaking out of resistance level with strong volume. RSI indicates upward momentum without overbought conditions.',
              confidenceScore: 72
            },
            {
              agentType: 'SentimentAnalyst',
              analysis: 'Social media sentiment is overwhelmingly positive with 78% bullish mentions. Recent news coverage is favorable.',
              confidenceScore: 65
            }
          ],
          debate: {
            bullishArguments: [
              {
                point: 'Strong earnings growth trajectory',
                evidence: 'EPS growth of 12% YoY with upward guidance revisions'
              },
              {
                point: 'Technical breakout pattern',
                evidence: 'Price breaking above 50-day moving average with increased volume'
              }
            ],
            bearishArguments: [
              {
                point: 'High valuation concerns',
                evidence: 'P/E ratio of 28.5 is above industry average of 22.3'
              },
              {
                point: 'Market volatility risks',
                evidence: 'Increased market volatility index suggests potential downside'
              }
            ],
            consensus: 'Moderately bullish with strong fundamentals offsetting valuation concerns. Technical breakout provides additional confirmation.'
          },
          riskAssessment: {
            riskScore: 45,
            riskLevel: 'Moderate',
            recommendedPositionSize: 5.0,
            suggestedStopLoss: 168.20,
            suggestedTakeProfit: 192.75,
            riskFactors: [
              'Market volatility',
              'Sector concentration',
              'Valuation concerns'
            ]
          }
        }
      } catch (error) {
        console.error('Analysis failed:', error)
      } finally {
        this.analyzing = false
      }
    },
    toggleAgent(agent) {
      this.enabledAgents[agent] = !this.enabledAgents[agent]
    },
    getAgentName(agentType) {
      const names = {
        FundamentalAnalyst: 'Fundamental Analyst',
        TechnicalAnalyst: 'Technical Analyst',
        SentimentAnalyst: 'Sentiment Analyst',
        NewsAnalyst: 'News Analyst',
        BullishResearcher: 'Bullish Researcher',
        BearishResearcher: 'Bearish Researcher',
        Trader: 'Trader Agent',
        RiskManager: 'Risk Manager'
      }
      return names[agentType] || agentType
    },
    confidenceClass(value) {
      if (value >= 70) return 'bg-emerald-400'
      if (value >= 40) return 'bg-amber-400'
      return 'bg-rose-400'
    },
    confidenceBadgeClass(value) {
      if (value >= 70) return 'border border-emerald-400/40 bg-emerald-500/15 text-emerald-100'
      if (value >= 40) return 'border border-amber-400/40 bg-amber-500/15 text-amber-100'
      return 'border border-rose-400/40 bg-rose-500/15 text-rose-100'
    },
    riskClass(score) {
      if (score <= 30) return 'bg-emerald-400'
      if (score <= 70) return 'bg-amber-400'
      return 'bg-rose-400'
    }
  }
}
</script>

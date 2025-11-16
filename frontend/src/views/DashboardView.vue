<template>
  <div class="space-y-10">
    <!-- Info notification for partial data -->
    <div v-if="!loading && Object.keys(quotes).length > 0 && Object.keys(quotes).length < 11" class="rounded-2xl border border-blue-500/30 bg-blue-500/10 p-4 text-sm text-blue-300">
      <div class="flex items-center gap-2">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <span>Showing available market data. Some quotes may be delayed or unavailable due to API rate limits.</span>
      </div>
    </div>

    <!-- Error notification -->
    <div v-if="error" class="rounded-2xl border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
      <div class="flex items-center gap-2">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <span class="font-medium">Error loading market data:</span>
        <span>{{ error }}</span>
      </div>
    </div>

    <!-- Health status indicator (only show if critical) -->
    <div v-if="healthStatus && healthStatus.status !== 'Healthy' && false" class="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-300">
      <div class="flex items-center gap-2">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <span class="font-medium">API Status:</span>
        <span>{{ healthStatus.details }}</span>
      </div>
    </div>

    <section class="overflow-hidden rounded-3xl border border-white/10 bg-gradient-to-br from-slate-900 via-slate-950 to-slate-900 p-8 shadow-[0_40px_120px_-60px_rgba(56,189,248,0.45)]">
      <div class="flex flex-col gap-8 lg:flex-row lg:items-center lg:justify-between">
        <div class="space-y-4">
          <span class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-4 py-1 text-xs uppercase tracking-[0.3em] text-slate-300">
            Live market signal
            <span class="h-2 w-2 animate-pulse rounded-full bg-emerald-400"></span>
          </span>
          <h1 class="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
            Market intelligence snapshot
          </h1>
          <p class="max-w-xl text-sm text-slate-300">
            Multi-source sentiment, quantitative signals, and macro factor scores distilled into a single control center.
          </p>
          <div class="flex flex-wrap gap-3 text-xs text-slate-300">
            <span class="rounded-full border border-emerald-400/40 bg-emerald-500/10 px-3 py-1 text-emerald-300">AI conviction: 78%</span>
            <span class="rounded-full border border-primary-500/30 bg-primary-500/10 px-3 py-1 text-primary-200">Volatility: 18.4</span>
            <span class="rounded-full border border-white/10 bg-white/5 px-3 py-1">Macro regime: Balanced</span>
          </div>
        </div>
        <div class="grid w-full max-w-sm gap-4">
          <div v-if="loading && !spxQuote" class="flex items-center justify-center rounded-2xl border border-white/10 bg-black/30 px-4 py-3">
            <span class="text-xs text-slate-400">Loading...</span>
          </div>
          <div v-else class="flex items-center justify-between rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-sm">
            <div class="text-slate-300">
              <p class="text-xs uppercase tracking-wide text-slate-400">S&P 500</p>
              <p class="text-lg font-semibold text-white">{{ spxQuote ? formatPrice(spxQuote.currentPrice) : 'N/A' }}</p>
            </div>
            <span v-if="spxQuote" :class="['rounded-full px-3 py-1', getChangeClass(spxQuote.changePercent)]">
              {{ formatPercent(spxQuote.changePercent) }}
            </span>
          </div>
          <div v-if="loading && !ndxQuote" class="flex items-center justify-center rounded-2xl border border-white/10 bg-black/30 px-4 py-3">
            <span class="text-xs text-slate-400">Loading...</span>
          </div>
          <div v-else class="flex items-center justify-between rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-sm">
            <div class="text-slate-300">
              <p class="text-xs uppercase tracking-wide text-slate-400">NASDAQ 100</p>
              <p class="text-lg font-semibold text-white">{{ ndxQuote ? formatPrice(ndxQuote.currentPrice) : 'N/A' }}</p>
            </div>
            <span v-if="ndxQuote" :class="['rounded-full px-3 py-1', getChangeClass(ndxQuote.changePercent)]">
              {{ formatPercent(ndxQuote.changePercent) }}
            </span>
          </div>
        </div>
      </div>
    </section>

    <section class="space-y-6">
      <h2 class="text-sm font-semibold uppercase tracking-[0.3em] text-slate-400">Market structures</h2>
      <div class="grid gap-6 lg:grid-cols-4">
        <article class="group relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-white/10 via-white/5 to-transparent p-6 shadow-soft">
          <div class="absolute inset-0 -z-10 opacity-0 transition duration-500 group-hover:opacity-100" style="background: radial-gradient(circle at top, rgba(72,127,255,0.25), transparent 55%);"></div>
          <p class="text-xs uppercase tracking-[0.25em] text-primary-200">Momentum</p>
          <h3 class="mt-3 text-3xl font-semibold text-white">15,245</h3>
          <p class="text-sm text-emerald-300">+0.75% • Bullish bias</p>
          <div class="mt-6 flex items-center justify-between text-xs text-slate-300">
            <span>Advance / Decline</span>
            <span>68 / 32</span>
          </div>
        </article>

        <article class="relative overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <p class="text-xs uppercase tracking-[0.25em] text-slate-400">Gainers</p>
          <ul v-if="gainers.length > 0" class="mt-4 space-y-3 text-sm text-slate-200">
            <li v-for="stock in gainers" :key="stock.symbol" class="flex items-center justify-between">
              <span class="font-medium">{{ stock.symbol }}</span>
              <span :class="['rounded-full px-3 py-1', getChangeClass(stock.changePercent)]">
                {{ formatPercent(stock.changePercent) }}
              </span>
            </li>
          </ul>
          <div v-else class="mt-4 text-sm text-slate-400">
            {{ loading ? 'Loading...' : 'No gainers available' }}
          </div>
        </article>

        <article class="relative overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <p class="text-xs uppercase tracking-[0.25em] text-slate-400">Losers</p>
          <ul v-if="losers.length > 0" class="mt-4 space-y-3 text-sm text-slate-200">
            <li v-for="stock in losers" :key="stock.symbol" class="flex items-center justify-between">
              <span class="font-medium">{{ stock.symbol }}</span>
              <span :class="['rounded-full px-3 py-1', getChangeClass(stock.changePercent)]">
                {{ formatPercent(stock.changePercent) }}
              </span>
            </li>
          </ul>
          <div v-else class="mt-4 text-sm text-slate-400">
            {{ loading ? 'Loading...' : 'No losers available' }}
          </div>
        </article>

        <article class="relative overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <p class="text-xs uppercase tracking-[0.25em] text-slate-400">Watchlist</p>
          <div v-if="watchlist.length > 0" class="mt-4 space-y-3 text-sm">
            <div v-for="stock in watchlist" :key="stock.symbol" class="flex items-center justify-between text-slate-200">
              <div>
                <p class="font-medium">{{ stock.symbol }}</p>
                <p class="text-xs text-slate-500">{{ stock.shortName || 'Technology' }}</p>
              </div>
              <span class="text-slate-300">{{ formatPrice(stock.currentPrice) }}</span>
            </div>
          </div>
          <div v-else class="mt-4 text-sm text-slate-400">
            {{ loading ? 'Loading...' : 'No watchlist items' }}
          </div>
        </article>
      </div>
    </section>

    <section class="grid gap-6 lg:grid-cols-[2fr_1fr]">
      <div class="overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 shadow-soft">
        <div class="flex items-center justify-between border-b border-white/5 px-6 py-4">
          <div>
            <h3 class="text-sm font-semibold tracking-wide text-white">Macro factor heatmap</h3>
            <p class="text-xs text-slate-400">Synthetic blend of volatility, breadth, momentum, liquidity</p>
          </div>
          <button class="rounded-full border border-white/10 px-3 py-1 text-xs text-slate-300 transition hover:border-primary-400 hover:text-primary-200">Export</button>
        </div>
        <div class="grid gap-4 p-6 text-xs text-slate-300 md:grid-cols-2 xl:grid-cols-4">
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-slate-400">Volatility</p>
            <p class="mt-3 text-lg font-semibold text-white">18.4</p>
            <p class="text-emerald-300">Cooling</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-slate-400">Breadth</p>
            <p class="mt-3 text-lg font-semibold text-white">62%</p>
            <p class="text-slate-300">Expanding</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-slate-400">Sentiment</p>
            <p class="mt-3 text-lg font-semibold text-white">Positive</p>
            <p class="text-emerald-300">71% bullish</p>
          </div>
          <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
            <p class="text-slate-400">Liquidity</p>
            <p class="mt-3 text-lg font-semibold text-white">Normal</p>
            <p class="text-slate-300">Volume 1.3× avg</p>
          </div>
        </div>
        <div class="border-t border-white/5 bg-slate-950/60 px-6 py-4 text-xs text-slate-400">
          Updated 6 minutes ago • Powered by multi-agent inference and cross-market data feeds.
        </div>
      </div>

      <aside class="space-y-4">
        <div class="rounded-3xl border border-white/5 bg-primary-500/10 p-6 text-sm text-primary-100">
          <p class="text-xs uppercase tracking-[0.3em]">AI highlights</p>
          <h3 class="mt-3 text-lg font-semibold text-white">AAPL debate consensus</h3>
          <p class="mt-2 text-primary-50/80">
            Multi-agent vote: BUY with 78% confidence. Key catalyst: accelerated services margin and AI hardware refresh.
          </p>
        </div>
        <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 text-sm text-slate-300">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-500">Upcoming events</p>
          <ul class="mt-4 space-y-3">
            <li class="flex items-center justify-between">
              <span>Fed minutes release</span>
              <span class="rounded-full bg-white/5 px-3 py-1 text-xs">Wed 2:00 PM ET</span>
            </li>
            <li class="flex items-center justify-between">
              <span>NVIDIA earnings</span>
              <span class="rounded-full bg-white/5 px-3 py-1 text-xs">Thu after close</span>
            </li>
            <li class="flex items-center justify-between">
              <span>Options expiry</span>
              <span class="rounded-full bg-white/5 px-3 py-1 text-xs">Fri open</span>
            </li>
          </ul>
        </div>
      </aside>
    </section>

    <section class="grid gap-6 lg:grid-cols-2">
      <article class="overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 shadow-soft">
        <div class="flex items-center justify-between border-b border-white/5 px-6 py-4">
          <div>
            <h3 class="text-sm font-semibold text-white">Top news pulse</h3>
            <p class="text-xs text-slate-400">Curated headlines driving sentiment</p>
          </div>
          <button class="rounded-full border border-white/10 px-3 py-1 text-xs text-slate-300 transition hover:border-primary-400 hover:text-primary-200">Open feed</button>
        </div>
        <div class="divide-y divide-white/5 text-sm text-slate-200">
          <div class="space-y-1 px-6 py-4">
            <p class="font-medium text-white">Tech megacaps extend rally on AI optimism</p>
            <p class="text-xs text-slate-400">Bloomberg • 1h ago</p>
          </div>
          <div class="space-y-1 px-6 py-4">
            <p class="font-medium text-white">Semiconductor supply tightens as demand accelerates</p>
            <p class="text-xs text-slate-400">Reuters • 3h ago</p>
          </div>
          <div class="space-y-1 px-6 py-4">
            <p class="font-medium text-white">Macro watch: US CPI print shows controlled disinflation</p>
            <p class="text-xs text-slate-400">WSJ • 5h ago</p>
          </div>
        </div>
      </article>

      <article class="overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 shadow-soft">
        <div class="flex items-center justify-between border-b border-white/5 px-6 py-4">
          <div>
            <h3 class="text-sm font-semibold text-white">Agent signals</h3>
            <p class="text-xs text-slate-400">Live instructions from AI trade desk</p>
          </div>
          <span class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">Updated 2m ago</span>
        </div>
        <div class="divide-y divide-white/5 text-sm text-slate-200">
          <div class="flex items-center justify-between px-6 py-4">
            <div>
              <p class="font-medium text-white">Fundamental Analyst • AAPL</p>
              <p class="text-xs text-slate-400">Upside revisions across services margin</p>
            </div>
            <span class="rounded-full bg-emerald-500/10 px-3 py-1 text-emerald-300">BUY</span>
          </div>
          <div class="flex items-center justify-between px-6 py-4">
            <div>
              <p class="font-medium text-white">Technical Analyst • TSLA</p>
              <p class="text-xs text-slate-400">Momentum breakdown near 21 DMA</p>
            </div>
            <span class="rounded-full bg-rose-500/10 px-3 py-1 text-rose-300">TRIM</span>
          </div>
          <div class="flex items-center justify-between px-6 py-4">
            <div>
              <p class="font-medium text-white">Sentiment Analyst • NVDA</p>
              <p class="text-xs text-slate-400">Social buzz +24% week-over-week</p>
            </div>
            <span class="rounded-full bg-primary-500/10 px-3 py-1 text-primary-200">ACCUMULATE</span>
          </div>
        </div>
      </article>
    </section>

    <!-- Rate Limit Metrics (for debugging/monitoring) -->
    <section v-if="metrics" class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
      <div class="flex items-center justify-between border-b border-white/5 pb-4">
        <div>
          <h3 class="text-sm font-semibold text-white">API Rate Limit Status</h3>
          <p class="text-xs text-slate-400">Real-time monitoring of Yahoo Finance API usage</p>
        </div>
        <button @click="fetchHealthData" class="rounded-full border border-white/10 px-3 py-1 text-xs text-slate-300 transition hover:border-primary-400 hover:text-primary-200">
          Refresh
        </button>
      </div>
      <div class="mt-4 grid gap-4 text-xs md:grid-cols-4">
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-slate-400">Total Requests</p>
          <p class="mt-2 text-2xl font-semibold text-white">{{ metrics.totalRequests.toLocaleString() }}</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-slate-400">Rate Limit Hits</p>
          <p class="mt-2 text-2xl font-semibold text-white">{{ metrics.totalRateLimitHits.toLocaleString() }}</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-slate-400">Hit Rate</p>
          <p class="mt-2 text-2xl font-semibold text-white">{{ (metrics.rateLimitHitRate * 100).toFixed(2) }}%</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-slate-400">Uptime</p>
          <p class="mt-2 text-lg font-semibold text-white">{{ formatUptime(metrics.uptime) }}</p>
        </div>
      </div>
      <div v-if="Object.keys(metrics.requestsByEndpoint).length > 0" class="mt-4">
        <p class="mb-2 text-xs uppercase tracking-wide text-slate-400">Requests by Endpoint</p>
        <div class="grid gap-2 text-xs md:grid-cols-3">
          <div v-for="(count, endpoint) in metrics.requestsByEndpoint" :key="endpoint" class="flex items-center justify-between rounded-lg border border-white/5 bg-white/5 px-3 py-2">
            <span class="text-slate-300">{{ endpoint }}</span>
            <span class="font-semibold text-white">{{ count.toLocaleString() }}</span>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useMarketData } from '../composables/useMarketData'
import { healthService } from '../services/api'

const { quotes, loading, error, fetchMultipleQuotes, formatPrice, formatPercent } = useMarketData()

// Market indices and watchlist symbols
const indexSymbols = ['^GSPC', '^NDX'] // S&P 500, NASDAQ 100
const topSymbols = ['AAPL', 'MSFT', 'NVDA', 'GOOGL', 'TSLA', 'AMZN']
const watchlistSymbols = ['NVDA', 'AMD', 'SMCI']

const healthStatus = ref(null)
const metrics = ref(null)
const loadingHealth = ref(false)

// Computed properties for market data
const spxQuote = computed(() => quotes.value['^GSPC'])
const ndxQuote = computed(() => quotes.value['^NDX'])

const topStocks = computed(() => {
  return topSymbols
    .map(symbol => quotes.value[symbol])
    .filter(q => q != null)
    .sort((a, b) => (b.changePercent || 0) - (a.changePercent || 0))
})

const gainers = computed(() => topStocks.value.filter(q => (q.changePercent || 0) > 0).slice(0, 3))
const losers = computed(() => topStocks.value.filter(q => (q.changePercent || 0) < 0).slice(0, 3))

const watchlist = computed(() => {
  return watchlistSymbols
    .map(symbol => quotes.value[symbol])
    .filter(q => q != null)
})

const getChangeClass = (changePercent) => {
  if (changePercent == null) return 'bg-white/5 text-slate-300'
  return changePercent >= 0 
    ? 'bg-emerald-500/10 text-emerald-300' 
    : 'bg-rose-500/10 text-rose-300'
}

const formatUptime = (uptime) => {
  if (!uptime) return 'N/A'
  // uptime is in format "HH:MM:SS" or TimeSpan format
  if (typeof uptime === 'string') {
    return uptime
  }
  // If it's an object with hours, minutes, seconds
  if (uptime.hours !== undefined) {
    return `${uptime.hours}h ${uptime.minutes}m ${uptime.seconds}s`
  }
  return uptime.toString()
}

const fetchMarketData = async () => {
  try {
    const allSymbols = [...indexSymbols, ...topSymbols, ...watchlistSymbols]
    await fetchMultipleQuotes(allSymbols)
    
    // Log successful fetches
    const successCount = Object.keys(quotes.value).length
    console.log(`Successfully fetched ${successCount} of ${allSymbols.length} quotes`)
  } catch (err) {
    console.error('Error fetching market data:', err)
    // Don't set error state, show what data we have
  }
}

const fetchHealthData = async () => {
  loadingHealth.value = true
  try {
    const [health, metricsData] = await Promise.allSettled([
      healthService.getHealth(),
      healthService.getMetrics()
    ])
    
    if (health.status === 'fulfilled') {
      healthStatus.value = health.value
    }
    if (metricsData.status === 'fulfilled') {
      metrics.value = metricsData.value
    }
  } catch (err) {
    console.error('Error fetching health data:', err)
    // Don't show error to user, health check is not critical
  } finally {
    loadingHealth.value = false
  }
}

onMounted(() => {
  fetchMarketData()
  fetchHealthData()
  
  // Refresh market data every 60 seconds
  const interval = setInterval(fetchMarketData, 60000)
  
  // Cleanup on unmount
  return () => clearInterval(interval)
})
</script>

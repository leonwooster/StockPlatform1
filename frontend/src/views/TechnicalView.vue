<template>
  <div class="space-y-10">
    <section class="rounded-3xl border border-white/5 bg-slate-900/70 p-8 shadow-soft">
      <div class="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
        <div class="space-y-4">
          <h1 class="text-3xl font-semibold tracking-tight text-white">Technical Analysis</h1>
          <p class="max-w-xl text-sm text-slate-300">
            Overlay multi-timeframe signals, momentum metrics, and volatility factors to decode market structure in seconds.
          </p>
        </div>
        <div class="w-full max-w-md space-y-3 rounded-2xl border border-white/10 bg-slate-950/60 p-4">
          <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Symbol</label>
          <div class="flex gap-3">
            <input
              v-model="selectedSymbol"
              type="text"
              class="flex-1 rounded-xl border border-white/10 bg-white/5 px-4 py-3 text-sm tracking-wide text-white placeholder:text-slate-500 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40"
              placeholder="e.g. AAPL"
              @keyup.enter="loadChartData"
            >
            <button
              @click="loadChartData"
              class="rounded-xl bg-primary-500 px-4 py-3 text-sm font-semibold text-white shadow-soft transition hover:bg-primary-400"
            >
              Load chart
            </button>
          </div>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="period in timePeriods"
              :key="period"
              @click="setTimePeriod(period)"
              class="rounded-full border px-3 py-1.5 text-xs font-medium transition"
              :class="currentTimePeriod === period
                ? 'border-primary-400 bg-primary-500/20 text-primary-100'
                : 'border-white/10 bg-white/5 text-slate-300 hover:border-primary-300/40 hover:text-primary-100'"
            >
              {{ period }}
            </button>
          </div>
        </div>
      </div>
      <div class="mt-8 grid gap-6 lg:grid-cols-[3fr_2fr]">
        <div class="rounded-3xl border border-dashed border-white/15 bg-slate-950/30 p-6 text-center">
          <div class="mx-auto h-72 w-full max-w-4xl rounded-2xl border border-white/10 bg-[radial-gradient(circle_at_top,_rgba(72,127,255,0.12),_transparent_65%)] text-sm text-slate-400">
            <div class="flex h-full flex-col items-center justify-center gap-2">
              <span class="rounded-full border border-white/10 px-3 py-1 text-xs uppercase tracking-[0.3em] text-slate-500">Chart surface</span>
              <p>ECharts candlestick + overlays render here</p>
            </div>
          </div>
        </div>
        <aside class="grid gap-4 text-sm text-slate-200">
          <article class="rounded-2xl border border-white/10 bg-white/5 p-5">
            <h3 class="text-xs uppercase tracking-[0.3em] text-slate-500">Active indicators</h3>
            <div class="mt-3 space-y-2">
              <label class="flex items-center gap-2">
                <input type="checkbox" class="h-4 w-4 rounded border-white/20 bg-slate-900/80 text-primary-400 focus:ring-primary-400" checked>
                <span>RSI (14)</span>
              </label>
              <label class="flex items-center gap-2">
                <input type="checkbox" class="h-4 w-4 rounded border-white/20 bg-slate-900/80 text-primary-400 focus:ring-primary-400" checked>
                <span>MACD (12,26,9)</span>
              </label>
              <label class="flex items-center gap-2">
                <input type="checkbox" class="h-4 w-4 rounded border-white/20 bg-slate-900/80 text-primary-400 focus:ring-primary-400">
                <span>Bollinger Bands</span>
              </label>
              <label class="flex items-center gap-2">
                <input type="checkbox" class="h-4 w-4 rounded border-white/20 bg-slate-900/80 text-primary-400 focus:ring-primary-400">
                <span>EMA (50/200)</span>
              </label>
            </div>
          </article>
          <article class="rounded-2xl border border-emerald-400/20 bg-emerald-500/10 p-5">
            <h3 class="text-xs uppercase tracking-[0.3em] text-emerald-200">Signal strength</h3>
            <div class="mt-3 flex items-end gap-3">
              <div class="h-12 w-12 rounded-full bg-emerald-500/20 text-center text-lg font-semibold text-emerald-200">72%</div>
              <div class="flex-1 space-y-2">
                <div class="w-full rounded-full bg-white/10">
                  <div class="rounded-full bg-emerald-400 py-1" style="width: 72%"></div>
                </div>
                <p class="text-xs text-emerald-100/90">Momentum aligned across medium timeframes — strong buy bias.</p>
              </div>
            </div>
          </article>
          <article class="rounded-2xl border border-white/10 bg-white/5 p-5">
            <h3 class="text-xs uppercase tracking-[0.3em] text-slate-500">Price map</h3>
            <div class="mt-3 space-y-2 text-sm">
              <div class="flex items-center justify-between">
                <span>Resistance 1</span>
                <span class="font-semibold text-slate-100">$180.50</span>
              </div>
              <div class="flex items-center justify-between">
                <span>Current</span>
                <span class="font-semibold text-primary-200">$175.25</span>
              </div>
              <div class="flex items-center justify-between">
                <span>Support 1</span>
                <span class="font-semibold text-slate-100">$170.75</span>
              </div>
            </div>
          </article>
        </aside>
      </div>
    </section>

    <section class="grid gap-6 lg:grid-cols-2">
      <article class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-sm font-semibold text-white">RSI monitor</h2>
            <p class="text-xs text-slate-400">14-period oscillator with dynamic zones</p>
          </div>
          <span class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">Neutral</span>
        </div>
        <div class="mt-5 h-64 rounded-2xl border border-dashed border-white/15 bg-slate-950/40"></div>
        <div class="mt-5 text-sm text-slate-300">
          <p class="text-center font-medium text-white">RSI value: 62.5</p>
          <p class="mt-1 text-center text-xs text-slate-400">Momentum cooling from overbought region — watch for cross below 60.</p>
        </div>
      </article>

      <article class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-sm font-semibold text-white">MACD insight</h2>
            <p class="text-xs text-slate-400">12/26 EMAs with 9-period signal overlay</p>
          </div>
          <span class="rounded-full border border-primary-400/40 bg-primary-500/10 px-3 py-1 text-xs text-primary-200">Bullish crossover</span>
        </div>
        <div class="mt-5 h-64 rounded-2xl border border-dashed border-white/15 bg-slate-950/40"></div>
        <div class="mt-5 text-sm text-slate-300">
          <p class="text-center font-medium text-white">MACD: 1.25 • Signal: 0.85</p>
          <p class="mt-1 text-center text-xs text-slate-400">Histogram expanding for 4 sessions — confirming upside acceleration.</p>
        </div>
      </article>
    </section>
  </div>
</template>

<script>
export default {
  name: 'TechnicalView',
  data() {
    return {
      selectedSymbol: 'AAPL',
      currentTimePeriod: '1D',
      timePeriods: ['1D', '1W', '1M', '3M', '1Y', '5Y']
    }
  },
  methods: {
    loadChartData() {
      console.log(`Loading chart data for ${this.selectedSymbol}`)
    },
    setTimePeriod(period) {
      this.currentTimePeriod = period
      this.loadChartData()
    }
  }
}
</script>

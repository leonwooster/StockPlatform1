<template>
  <div class="space-y-10">
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
          <div class="flex items-center justify-between rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-sm">
            <div class="text-slate-300">
              <p class="text-xs uppercase tracking-wide text-slate-400">S&P 500</p>
              <p class="text-lg font-semibold text-white">4,938.12</p>
            </div>
            <span class="rounded-full bg-emerald-500/15 px-3 py-1 text-emerald-300">+0.84%</span>
          </div>
          <div class="flex items-center justify-between rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-sm">
            <div class="text-slate-300">
              <p class="text-xs uppercase tracking-wide text-slate-400">NASDAQ 100</p>
              <p class="text-lg font-semibold text-white">15,245.67</p>
            </div>
            <span class="rounded-full bg-emerald-500/15 px-3 py-1 text-emerald-300">+1.12%</span>
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
          <ul class="mt-4 space-y-3 text-sm text-slate-200">
            <li class="flex items-center justify-between">
              <span class="font-medium">AAPL</span>
              <span class="rounded-full bg-emerald-500/10 px-3 py-1 text-emerald-300">+2.3%</span>
            </li>
            <li class="flex items-center justify-between">
              <span class="font-medium">MSFT</span>
              <span class="rounded-full bg-emerald-500/10 px-3 py-1 text-emerald-300">+1.8%</span>
            </li>
            <li class="flex items-center justify-between">
              <span class="font-medium">NVDA</span>
              <span class="rounded-full bg-emerald-500/10 px-3 py-1 text-emerald-300">+1.4%</span>
            </li>
          </ul>
        </article>

        <article class="relative overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <p class="text-xs uppercase tracking-[0.25em] text-slate-400">Losers</p>
          <ul class="mt-4 space-y-3 text-sm text-slate-200">
            <li class="flex items-center justify-between">
              <span class="font-medium">GOOGL</span>
              <span class="rounded-full bg-rose-500/10 px-3 py-1 text-rose-300">-1.2%</span>
            </li>
            <li class="flex items-center justify-between">
              <span class="font-medium">TSLA</span>
              <span class="rounded-full bg-rose-500/10 px-3 py-1 text-rose-300">-0.9%</span>
            </li>
            <li class="flex items-center justify-between">
              <span class="font-medium">AMZN</span>
              <span class="rounded-full bg-rose-500/10 px-3 py-1 text-rose-300">-0.6%</span>
            </li>
          </ul>
        </article>

        <article class="relative overflow-hidden rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <p class="text-xs uppercase tracking-[0.25em] text-slate-400">Watchlist</p>
          <div class="mt-4 space-y-3 text-sm">
            <div class="flex items-center justify-between text-slate-200">
              <div>
                <p class="font-medium">NVDA</p>
                <p class="text-xs text-slate-500">AI infrastructure</p>
              </div>
              <span class="text-slate-300">$425.60</span>
            </div>
            <div class="flex items-center justify-between text-slate-200">
              <div>
                <p class="font-medium">AMD</p>
                <p class="text-xs text-slate-500">Data center</p>
              </div>
              <span class="text-slate-300">$115.30</span>
            </div>
            <div class="flex items-center justify-between text-slate-200">
              <div>
                <p class="font-medium">SMCI</p>
                <p class="text-xs text-slate-500">HPC systems</p>
              </div>
              <span class="text-slate-300">$904.10</span>
            </div>
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
  </div>
</template>

<script>
export default {
  name: 'DashboardView'
}
</script>

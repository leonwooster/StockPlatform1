<template>
  <div class="space-y-10">
    <section class="rounded-3xl border border-white/5 bg-slate-900/60 p-8 shadow-soft">
      <div class="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 class="text-3xl font-semibold tracking-tight text-white">Stock Screener</h1>
          <p class="mt-2 max-w-2xl text-sm text-slate-300">
            Build institutional-grade filters blending fundamentals, momentum, and macro overlays. Save custom universes or deploy with one click.
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-3 text-xs text-slate-300">
          <span class="rounded-full border border-primary-400/30 bg-primary-500/10 px-3 py-1 text-primary-200">24 results</span>
          <span class="rounded-full border border-white/10 bg-white/5 px-3 py-1">Universe: US Equities</span>
          <span class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-3 py-1 text-emerald-200">AI overlay active</span>
        </div>
      </div>

      <div class="mt-8 grid gap-6 lg:grid-cols-4">
        <article class="space-y-3 rounded-2xl border border-white/10 bg-white/5 p-5 text-sm text-slate-200">
          <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Market cap</label>
          <select class="w-full rounded-xl border border-white/10 bg-slate-950/50 px-4 py-3 text-sm text-slate-200 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            <option>Mega ($200B+)</option>
            <option>Large ($10B-$200B)</option>
            <option>Mid ($2B-$10B)</option>
            <option>Small ($300M-$2B)</option>
          </select>
        </article>
        <article class="space-y-3 rounded-2xl border border-white/10 bg-white/5 p-5 text-sm text-slate-200">
          <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Price range</label>
          <div class="flex items-center gap-3">
            <input type="number" placeholder="Min" class="w-full rounded-xl border border-white/10 bg-slate-950/50 px-4 py-3 text-sm text-slate-200 placeholder:text-slate-500 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            <input type="number" placeholder="Max" class="w-full rounded-xl border border-white/10 bg-slate-950/50 px-4 py-3 text-sm text-slate-200 placeholder:text-slate-500 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
          </div>
        </article>
        <article class="space-y-3 rounded-2xl border border-white/10 bg-white/5 p-5 text-sm text-slate-200">
          <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Sector</label>
          <select class="w-full rounded-xl border border-white/10 bg-slate-950/50 px-4 py-3 text-sm text-slate-200 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            <option>Technology</option>
            <option>Healthcare</option>
            <option>Financials</option>
            <option>Consumer</option>
          </select>
        </article>
        <article class="space-y-3 rounded-2xl border border-white/10 bg-white/5 p-5 text-sm text-slate-200">
          <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Performance</label>
          <select class="w-full rounded-xl border border-white/10 bg-slate-950/50 px-4 py-3 text-sm text-slate-200 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            <option>Up 10%+</option>
            <option>Up 20%+</option>
            <option>Down 10%+</option>
            <option>Down 20%+</option>
          </select>
        </article>
      </div>

      <div class="mt-6 flex flex-wrap gap-3 text-sm">
        <button class="rounded-full bg-primary-500 px-5 py-2 text-white shadow-soft transition hover:bg-primary-400">Run screen</button>
        <button class="rounded-full border border-white/10 px-5 py-2 text-slate-200 transition hover:border-primary-300/40 hover:text-primary-100">Save template</button>
        <button class="rounded-full border border-white/10 px-5 py-2 text-slate-200 transition hover:border-primary-300/40 hover:text-primary-100">Load screen</button>
      </div>
    </section>

    <section class="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
      <article class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-sm font-semibold text-white">Intrinsic value calculator</h2>
            <p class="text-xs text-slate-400">Blended DCF / multiples estimator</p>
          </div>
          <span class="rounded-full border border-emerald-400/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">Undervalued</span>
        </div>
        <div class="mt-6 grid gap-6 md:grid-cols-2">
          <div class="space-y-3 text-sm text-slate-200">
            <div>
              <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Current price</label>
              <input type="number" value="175.00" class="mt-1 w-full rounded-xl border border-white/10 bg-white/5 px-4 py-3 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            </div>
            <div>
              <label class="text-xs uppercase tracking-[0.3em] text-slate-500">EPS (TTM)</label>
              <input type="number" value="6.15" class="mt-1 w-full rounded-xl border border-white/10 bg-white/5 px-4 py-3 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            </div>
            <div>
              <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Growth rate (%)</label>
              <input type="number" value="8.5" class="mt-1 w-full rounded-xl border border-white/10 bg-white/5 px-4 py-3 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            </div>
            <div>
              <label class="text-xs uppercase tracking-[0.3em] text-slate-500">Discount rate (%)</label>
              <input type="number" value="10" class="mt-1 w-full rounded-xl border border-white/10 bg-white/5 px-4 py-3 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500/40">
            </div>
          </div>
          <div class="rounded-2xl border border-primary-400/30 bg-primary-500/10 p-5 text-sm text-primary-100">
            <div class="flex items-center justify-between">
              <span>Intrinsic value</span>
              <span class="text-lg font-semibold text-white">$215.75</span>
            </div>
            <div class="mt-3 flex items-center justify-between">
              <span>Margin of safety</span>
              <span class="rounded-full bg-emerald-500/20 px-3 py-1 text-emerald-100">23.3%</span>
            </div>
            <p class="mt-4 text-xs text-primary-50/80">
              AI-assisted valuation indicates attractive upside with resilient cash-flow profile.
            </p>
          </div>
        </div>
      </article>

      <aside class="space-y-4">
        <div class="rounded-3xl border border-white/5 bg-white/5 p-6 text-sm text-slate-200">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-500">Saved Playbooks</p>
          <ul class="mt-3 space-y-2">
            <li class="flex items-center justify-between rounded-xl border border-white/10 bg-slate-950/40 px-4 py-3">
              <span>AI Growth Momentum</span>
              <button class="rounded-full border border-primary-400/40 px-3 py-1 text-xs text-primary-200">Load</button>
            </li>
            <li class="flex items-center justify-between rounded-xl border border-white/10 bg-slate-950/40 px-4 py-3">
              <span>Dividend Quality</span>
              <button class="rounded-full border border-primary-400/40 px-3 py-1 text-xs text-primary-200">Load</button>
            </li>
          </ul>
        </div>
        <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 text-sm text-slate-200">
          <p class="text-xs uppercase tracking-[0.3em] text-slate-500">Screener Insights</p>
          <p class="mt-3 text-xs text-slate-300">
            AI agents flag 68% of current screen constituents with positive earnings revisions. Consider increasing weighting to SMCI, CRM, NOW.
          </p>
        </div>
      </aside>
    </section>

    <section class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="text-sm font-semibold text-white">Screen results</h2>
          <p class="text-xs text-slate-400">Sorted by composite quality score</p>
        </div>
        <div class="flex items-center gap-2 text-xs text-slate-300">
          <button class="rounded-full border border-white/10 px-3 py-1 hover:border-primary-300/40 hover:text-primary-100">Download CSV</button>
          <button class="rounded-full border border-white/10 px-3 py-1 hover:border-primary-300/40 hover:text-primary-100">Push to watchlist</button>
        </div>
      </div>

      <div class="mt-5 overflow-x-auto">
        <table class="min-w-full divide-y divide-white/10 text-sm text-slate-200">
          <thead class="bg-white/5">
            <tr>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Symbol</th>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Company</th>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Price</th>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Change</th>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Mkt Cap</th>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">P/E</th>
              <th class="px-5 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr class="hover:bg-white/5">
              <td class="px-5 py-4 font-semibold text-white">NVDA</td>
              <td class="px-5 py-4">NVIDIA Corp</td>
              <td class="px-5 py-4">$425.60</td>
              <td class="px-5 py-4 text-emerald-300">+3.25%</td>
              <td class="px-5 py-4">$1.06T</td>
              <td class="px-5 py-4">85.42</td>
              <td class="px-5 py-4">
                <button class="rounded-full border border-primary-400/40 px-3 py-1 text-xs text-primary-200 hover:border-primary-300 hover:text-primary-100">Analyze</button>
              </td>
            </tr>
            <tr class="hover:bg-white/5">
              <td class="px-5 py-4 font-semibold text-white">AMD</td>
              <td class="px-5 py-4">Advanced Micro Devices</td>
              <td class="px-5 py-4">$115.30</td>
              <td class="px-5 py-4 text-emerald-300">+1.80%</td>
              <td class="px-5 py-4">$142B</td>
              <td class="px-5 py-4">210.5</td>
              <td class="px-5 py-4">
                <button class="rounded-full border border-primary-400/40 px-3 py-1 text-xs text-primary-200 hover:border-primary-300 hover:text-primary-100">Analyze</button>
              </td>
            </tr>
            <tr class="hover:bg-white/5">
              <td class="px-5 py-4 font-semibold text-white">TSLA</td>
              <td class="px-5 py-4">Tesla Inc</td>
              <td class="px-5 py-4">$245.75</td>
              <td class="px-5 py-4 text-rose-300">-0.95%</td>
              <td class="px-5 py-4">$780B</td>
              <td class="px-5 py-4">65.2</td>
              <td class="px-5 py-4">
                <button class="rounded-full border border-primary-400/40 px-3 py-1 text-xs text-primary-200 hover:border-primary-300 hover:text-primary-100">Analyze</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="mt-5 flex flex-col gap-4 text-xs text-slate-400 md:flex-row md:items-center md:justify-between">
        <p>Showing <span class="text-white">1</span> to <span class="text-white">3</span> of <span class="text-white">24</span> screened equities.</p>
        <div class="flex items-center gap-2">
          <button class="rounded-full border border-white/10 px-4 py-1 hover:border-primary-300/40 hover:text-primary-100">Previous</button>
          <button class="rounded-full border border-white/10 px-4 py-1 hover:border-primary-300/40 hover:text-primary-100">Next</button>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
export default {
  name: 'ScreenerView'
}
</script>

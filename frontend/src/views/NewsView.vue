<template>
  <div class="space-y-10">
    <section class="rounded-3xl border border-white/5 bg-slate-900/60 p-8 shadow-soft">
      <div class="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 class="text-3xl font-semibold tracking-tight text-white">Market News & Sentiment</h1>
          <p class="mt-2 max-w-2xl text-sm text-slate-300">
            Real-time coverage fused with AI-powered sentiment models across financial media, research terminals, and social feeds.
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-3 text-xs text-slate-300">
          <span class="rounded-full border border-white/10 bg-white/5 px-3 py-1">Sources: Bloomberg, Reuters, CNBC, Social</span>
          <span class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-3 py-1 text-emerald-200">Sentiment: Bullish 62%</span>
        </div>
      </div>

      <div class="mt-6 grid gap-6 lg:grid-cols-[2fr_1fr]">
        <article class="rounded-3xl border border-white/5 bg-slate-950/60 p-6 text-sm text-slate-200">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-sm font-semibold text-white">Top headlines</h2>
              <p class="text-xs text-slate-400">Curated for AI trading signals</p>
            </div>
            <button class="rounded-full border border-white/10 px-3 py-1 text-xs text-slate-300 transition hover:border-primary-300/40 hover:text-primary-100">Refresh</button>
          </div>
          <div class="mt-6 space-y-5">
            <div class="rounded-2xl border border-white/5 bg-white/5 p-4 transition hover:border-primary-300/40">
              <div class="flex items-center justify-between text-xs text-slate-400">
                <span>Bloomberg • 2h ago</span>
                <span class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-2 py-1 text-emerald-200">Positive</span>
              </div>
              <h3 class="mt-3 text-lg font-semibold text-white">Fed policy meeting: probability of dovish tone rises</h3>
              <p class="mt-2 text-sm text-slate-300">Macro desk projects two cuts in 2026 as inflation cools without growth shock. Financials and growth factor lead gains.</p>
            </div>
            <div class="rounded-2xl border border-white/5 bg-white/5 p-4 transition hover:border-primary-300/40">
              <div class="flex items-center justify-between text-xs text-slate-400">
                <span>Reuters • 4h ago</span>
                <span class="rounded-full border border-emerald-400/30 bg-emerald-500/10 px-2 py-1 text-emerald-200">Very positive</span>
              </div>
              <h3 class="mt-3 text-lg font-semibold text-white">Tech megacaps extend rally after earnings beat</h3>
              <p class="mt-2 text-sm text-slate-300">Cloud and AI segments print double-digit growth with upside margin guidance, lifting NASDAQ futures.</p>
            </div>
            <div class="rounded-2xl border border-white/5 bg-white/5 p-4 transition hover:border-primary-300/40">
              <div class="flex items-center justify-between text-xs text-slate-400">
                <span>CNBC • 6h ago</span>
                <span class="rounded-full border border-amber-400/30 bg-amber-500/10 px-2 py-1 text-amber-200">Neutral</span>
              </div>
              <h3 class="mt-3 text-lg font-semibold text-white">Oil prices flare on renewed OPEC+ supply cuts</h3>
              <p class="mt-2 text-sm text-slate-300">Energy sector outperforms while airline basket softens. Watch inflation impulse through energy complex.</p>
            </div>
          </div>
          <div class="mt-6 text-center">
            <button class="rounded-full border border-white/10 px-5 py-2 text-xs text-slate-300 transition hover:border-primary-300/40 hover:text-primary-100">Load more headlines</button>
          </div>
        </article>

        <aside class="space-y-6">
          <article class="rounded-3xl border border-white/5 bg-white/5 p-6 text-sm text-slate-200">
            <h2 class="text-xs uppercase tracking-[0.3em] text-slate-500">Market sentiment</h2>
            <div class="mt-4 space-y-4">
              <div>
                <div class="flex items-center justify-between text-xs">
                  <span>Bullish</span>
                  <span>62%</span>
                </div>
                <div class="mt-1 h-2 w-full rounded-full bg-white/10">
                  <div class="h-2 rounded-full bg-emerald-400" style="width: 62%"></div>
                </div>
              </div>
              <div>
                <div class="flex items-center justify-between text-xs">
                  <span>Neutral</span>
                  <span>25%</span>
                </div>
                <div class="mt-1 h-2 w-full rounded-full bg-white/10">
                  <div class="h-2 rounded-full bg-amber-400" style="width: 25%"></div>
                </div>
              </div>
              <div>
                <div class="flex items-center justify-between text-xs">
                  <span>Bearish</span>
                  <span>13%</span>
                </div>
                <div class="mt-1 h-2 w-full rounded-full bg-white/10">
                  <div class="h-2 rounded-full bg-rose-400" style="width: 13%"></div>
                </div>
              </div>
            </div>
            <div class="mt-6 rounded-2xl border border-emerald-400/30 bg-emerald-500/10 px-4 py-3 text-xs text-emerald-100">
              Bias remains bullish with improving macro tone and persistent AI inflows.
            </div>
          </article>

          <article class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 text-sm text-slate-200">
            <h2 class="text-xs uppercase tracking-[0.3em] text-slate-500">Trending topics</h2>
            <div class="mt-3 space-y-3">
              <button class="flex w-full items-center justify-between rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-left transition hover:border-primary-300/40 hover:text-primary-100">
                <span>Fed meeting</span>
                <span class="rounded-full bg-primary-500/15 px-2 py-1 text-xs text-primary-200">+145%</span>
              </button>
              <button class="flex w-full items-center justify-between rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-left transition hover:border-primary-300/40 hover:text-primary-100">
                <span>AI earnings</span>
                <span class="rounded-full bg-primary-500/15 px-2 py-1 text-xs text-primary-200">+98%</span>
              </button>
              <button class="flex w-full items-center justify-between rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-left transition hover:border-primary-300/40 hover:text-primary-100">
                <span>OPEC+ policy</span>
                <span class="rounded-full bg-primary-500/15 px-2 py-1 text-xs text-primary-200">+32%</span>
              </button>
            </div>
          </article>
        </aside>
      </div>
    </section>

    <section class="grid gap-6 lg:grid-cols-3">
      <article class="rounded-3xl border border-white/5 bg-white/5 p-6 text-sm text-slate-200">
        <h2 class="text-xs uppercase tracking-[0.3em] text-slate-500">Social mentions</h2>
        <div class="mt-4 flex items-center justify-between">
          <div>
            <p class="text-base font-semibold text-white">Reddit</p>
            <p class="text-xs text-slate-400">r/wallstreetbets</p>
          </div>
          <div class="text-right">
            <p class="text-2xl font-semibold text-white">1,245</p>
            <p class="text-xs text-emerald-300">+15% vs. yesterday</p>
          </div>
        </div>
      </article>
      <article class="rounded-3xl border border-white/5 bg-white/5 p-6 text-sm text-slate-200">
        <h2 class="text-xs uppercase tracking-[0.3em] text-slate-500">Social mentions</h2>
        <div class="mt-4 flex items-center justify-between">
          <div>
            <p class="text-base font-semibold text-white">Twitter / X</p>
            <p class="text-xs text-slate-400">Finance & AI</p>
          </div>
          <div class="text-right">
            <p class="text-2xl font-semibold text-white">8,762</p>
            <p class="text-xs text-emerald-300">+8% vs. yesterday</p>
          </div>
        </div>
      </article>
      <article class="rounded-3xl border border-white/5 bg-white/5 p-6 text-sm text-slate-200">
        <h2 class="text-xs uppercase tracking-[0.3em] text-slate-500">Social mentions</h2>
        <div class="mt-4 flex items-center justify-between">
          <div>
            <p class="text-base font-semibold text-white">StockTwits</p>
            <p class="text-xs text-slate-400">Momentum chatter</p>
          </div>
          <div class="text-right">
            <p class="text-2xl font-semibold text-white">3,421</p>
            <p class="text-xs text-rose-300">-3% vs. yesterday</p>
          </div>
        </div>
      </article>
    </section>

    <section class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
      <div class="flex flex-col gap-4 text-xs text-slate-300 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 class="text-sm font-semibold text-white">Earnings calendar</h2>
          <p>Key reports in the next 72 hours</p>
        </div>
        <button class="self-start rounded-full border border-white/10 px-4 py-1 text-xs transition hover:border-primary-300/40 hover:text-primary-100 md:self-auto">Subscribe to alerts</button>
      </div>
      <div class="mt-5 overflow-x-auto">
        <table class="min-w-full divide-y divide-white/10 text-sm text-slate-200">
          <thead class="bg-white/5">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Date</th>
              <th class="px-6 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Symbol</th>
              <th class="px-6 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Company</th>
              <th class="px-6 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">EPS est.</th>
              <th class="px-6 py-3 text-left text-xs font-semibold uppercase tracking-[0.25em] text-slate-400">Revenue est.</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr class="hover:bg-white/5">
              <td class="px-6 py-4">Oct 19, 2025</td>
              <td class="px-6 py-4 font-semibold text-white">AAPL</td>
              <td class="px-6 py-4">Apple Inc.</td>
              <td class="px-6 py-4">$1.29</td>
              <td class="px-6 py-4">$89.5B</td>
            </tr>
            <tr class="hover:bg-white/5">
              <td class="px-6 py-4">Oct 20, 2025</td>
              <td class="px-6 py-4 font-semibold text-white">GOOGL</td>
              <td class="px-6 py-4">Alphabet Inc.</td>
              <td class="px-6 py-4">$1.52</td>
              <td class="px-6 py-4">$78.2B</td>
            </tr>
            <tr class="hover:bg-white/5">
              <td class="px-6 py-4">Oct 21, 2025</td>
              <td class="px-6 py-4 font-semibold text-white">AMZN</td>
              <td class="px-6 py-4">Amazon.com Inc.</td>
              <td class="px-6 py-4">$0.98</td>
              <td class="px-6 py-4">$145.0B</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script>
export default {
  name: 'NewsView'
}
</script>

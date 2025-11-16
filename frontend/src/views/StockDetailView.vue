<template>
  <div class="space-y-6">
    <!-- Stock Search -->
    <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
      <StockSearch @select="loadStock" />
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center rounded-3xl border border-white/5 bg-slate-900/60 p-12">
      <div class="text-center">
        <svg class="mx-auto h-12 w-12 animate-spin text-primary-400" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="mt-4 text-sm text-slate-400">Loading stock data...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-if="error" class="rounded-3xl border border-rose-500/30 bg-rose-500/10 p-6">
      <div class="flex items-center gap-3">
        <svg class="h-6 w-6 text-rose-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <div>
          <p class="font-medium text-rose-300">Error loading stock data</p>
          <p class="text-sm text-rose-200">{{ error }}</p>
        </div>
      </div>
    </div>

    <!-- Stock Data -->
    <div v-if="currentSymbol && !loading" class="space-y-6">
      <!-- Quote Section -->
      <section v-if="quote" class="overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-slate-900 via-slate-950 to-slate-900 p-8 shadow-soft">
        <div class="flex items-start justify-between">
          <div>
            <h1 class="text-3xl font-semibold text-white">{{ quote.symbol }}</h1>
            <p class="mt-1 text-slate-400">{{ quote.shortName || quote.longName }}</p>
          </div>
          <span :class="['rounded-full px-4 py-2 text-sm font-medium', getChangeClass(quote.changePercent)]">
            {{ formatPercent(quote.changePercent) }}
          </span>
        </div>
        <div class="mt-6 flex items-baseline gap-3">
          <p class="text-5xl font-bold text-white">{{ formatPrice(quote.currentPrice) }}</p>
          <p :class="['text-xl', quote.change >= 0 ? 'text-emerald-300' : 'text-rose-300']">
            {{ quote.change >= 0 ? '+' : '' }}{{ formatPrice(quote.change) }}
          </p>
        </div>
        <div class="mt-6 grid gap-4 text-sm md:grid-cols-4">
          <div>
            <p class="text-slate-400">Open</p>
            <p class="mt-1 font-semibold text-white">{{ formatPrice(quote.open) }}</p>
          </div>
          <div>
            <p class="text-slate-400">High</p>
            <p class="mt-1 font-semibold text-white">{{ formatPrice(quote.dayHigh) }}</p>
          </div>
          <div>
            <p class="text-slate-400">Low</p>
            <p class="mt-1 font-semibold text-white">{{ formatPrice(quote.dayLow) }}</p>
          </div>
          <div>
            <p class="text-slate-400">Volume</p>
            <p class="mt-1 font-semibold text-white">{{ formatVolume(quote.volume) }}</p>
          </div>
        </div>
      </section>

      <!-- Company Profile & Fundamentals -->
      <div class="grid gap-6 lg:grid-cols-2">
        <!-- Company Profile -->
        <section v-if="profile" class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <h2 class="text-lg font-semibold text-white">Company Profile</h2>
          <div class="mt-4 space-y-3 text-sm">
            <div>
              <p class="text-slate-400">Sector</p>
              <p class="mt-1 text-white">{{ profile.sector || 'N/A' }}</p>
            </div>
            <div>
              <p class="text-slate-400">Industry</p>
              <p class="mt-1 text-white">{{ profile.industry || 'N/A' }}</p>
            </div>
            <div>
              <p class="text-slate-400">Employees</p>
              <p class="mt-1 text-white">{{ profile.fullTimeEmployees?.toLocaleString() || 'N/A' }}</p>
            </div>
            <div>
              <p class="text-slate-400">Website</p>
              <a v-if="profile.website" :href="profile.website" target="_blank" class="mt-1 text-primary-300 hover:text-primary-200">
                {{ profile.website }}
              </a>
              <p v-else class="mt-1 text-white">N/A</p>
            </div>
            <div v-if="profile.description">
              <p class="text-slate-400">Description</p>
              <p class="mt-1 text-slate-300">{{ profile.description }}</p>
            </div>
          </div>
        </section>

        <!-- Fundamentals -->
        <section v-if="fundamentals" class="rounded-3xl border border-white/5 bg-slate-900/60 p-6 shadow-soft">
          <h2 class="text-lg font-semibold text-white">Key Metrics</h2>
          <div class="mt-4 grid gap-4 text-sm">
            <div class="flex items-center justify-between">
              <span class="text-slate-400">Market Cap</span>
              <span class="font-semibold text-white">{{ formatMarketCap(fundamentals.marketCap) }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-slate-400">P/E Ratio</span>
              <span class="font-semibold text-white">{{ fundamentals.peRatio?.toFixed(2) || 'N/A' }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-slate-400">EPS</span>
              <span class="font-semibold text-white">{{ formatPrice(fundamentals.eps) }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-slate-400">Dividend Yield</span>
              <span class="font-semibold text-white">{{ fundamentals.dividendYield ? (fundamentals.dividendYield * 100).toFixed(2) + '%' : 'N/A' }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-slate-400">Beta</span>
              <span class="font-semibold text-white">{{ fundamentals.beta?.toFixed(2) || 'N/A' }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-slate-400">52 Week High</span>
              <span class="font-semibold text-white">{{ formatPrice(fundamentals.fiftyTwoWeekHigh) }}</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-slate-400">52 Week Low</span>
              <span class="font-semibold text-white">{{ formatPrice(fundamentals.fiftyTwoWeekLow) }}</span>
            </div>
          </div>
        </section>
      </div>
    </div>

    <!-- Empty State -->
    <div v-if="!currentSymbol && !loading" class="flex items-center justify-center rounded-3xl border border-white/5 bg-slate-900/60 p-12">
      <div class="text-center">
        <svg class="mx-auto h-16 w-16 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="mt-4 text-lg font-medium text-slate-300">Search for a stock to get started</p>
        <p class="mt-2 text-sm text-slate-400">Enter a symbol or company name in the search box above</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import StockSearch from '../components/StockSearch.vue'
import { stockService } from '../services/api'
import { useMarketData } from '../composables/useMarketData'

const { formatPrice, formatPercent, formatVolume, formatMarketCap } = useMarketData()

const currentSymbol = ref(null)
const quote = ref(null)
const profile = ref(null)
const fundamentals = ref(null)
const loading = ref(false)
const error = ref(null)

const getChangeClass = (changePercent) => {
  if (changePercent == null) return 'bg-white/5 text-slate-300'
  return changePercent >= 0 
    ? 'bg-emerald-500/10 text-emerald-300' 
    : 'bg-rose-500/10 text-rose-300'
}

const loadStock = async (stock) => {
  currentSymbol.value = stock.symbol
  loading.value = true
  error.value = null
  quote.value = null
  profile.value = null
  fundamentals.value = null

  try {
    const [quoteData, profileData, fundamentalsData] = await Promise.allSettled([
      stockService.getQuote(stock.symbol),
      stockService.getCompanyProfile(stock.symbol),
      stockService.getFundamentals(stock.symbol)
    ])

    if (quoteData.status === 'fulfilled') {
      quote.value = quoteData.value
    }
    if (profileData.status === 'fulfilled') {
      profile.value = profileData.value
    }
    if (fundamentalsData.status === 'fulfilled') {
      fundamentals.value = fundamentalsData.value
    }

    if (quoteData.status === 'rejected' && profileData.status === 'rejected' && fundamentalsData.status === 'rejected') {
      throw new Error('Failed to load stock data')
    }
  } catch (err) {
    error.value = err.message
    console.error('Error loading stock:', err)
  } finally {
    loading.value = false
  }
}
</script>

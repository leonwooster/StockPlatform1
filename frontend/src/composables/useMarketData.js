import { ref, computed } from 'vue'
import { stockService } from '../services/api'

export function useMarketData() {
  const quotes = ref({})
  const loading = ref(false)
  const error = ref(null)

  const fetchQuote = async (symbol) => {
    loading.value = true
    error.value = null
    try {
      const quote = await stockService.getQuote(symbol)
      quotes.value[symbol] = quote
      return quote
    } catch (err) {
      error.value = err.message
      console.error(`Error fetching quote for ${symbol}:`, err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const fetchMultipleQuotes = async (symbols) => {
    loading.value = true
    error.value = null
    try {
      const results = await stockService.getQuotes(symbols)
      results.forEach(quote => {
        quotes.value[quote.symbol] = quote
      })
      return results
    } catch (err) {
      error.value = err.message
      console.error('Error fetching multiple quotes:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const getQuote = (symbol) => {
    return computed(() => quotes.value[symbol])
  }

  const formatPrice = (price) => {
    if (price == null) return 'N/A'
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(price)
  }

  const formatPercent = (value) => {
    if (value == null) return 'N/A'
    const sign = value >= 0 ? '+' : ''
    return `${sign}${value.toFixed(2)}%`
  }

  const formatVolume = (volume) => {
    if (volume == null) return 'N/A'
    if (volume >= 1e9) return `${(volume / 1e9).toFixed(2)}B`
    if (volume >= 1e6) return `${(volume / 1e6).toFixed(2)}M`
    if (volume >= 1e3) return `${(volume / 1e3).toFixed(2)}K`
    return volume.toString()
  }

  const formatMarketCap = (marketCap) => {
    if (marketCap == null) return 'N/A'
    if (marketCap >= 1e12) return `${(marketCap / 1e12).toFixed(2)}T`
    if (marketCap >= 1e9) return `${(marketCap / 1e9).toFixed(2)}B`
    if (marketCap >= 1e6) return `${(marketCap / 1e6).toFixed(2)}M`
    return marketCap.toString()
  }

  return {
    quotes,
    loading,
    error,
    fetchQuote,
    fetchMultipleQuotes,
    getQuote,
    formatPrice,
    formatPercent,
    formatVolume,
    formatMarketCap
  }
}

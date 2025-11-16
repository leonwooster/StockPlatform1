// Use HTTP for local development to avoid certificate issues
// Change to HTTPS in production
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

export const stockService = {
  async getStocks() {
    const response = await fetch(`${API_BASE_URL}/stocks`)
    if (!response.ok) {
      throw new Error('Failed to fetch stocks')
    }
    return response.json()
  },
  
  async getStockBySymbol(symbol) {
    const response = await fetch(`${API_BASE_URL}/stocks/${symbol}`)
    if (!response.ok) {
      throw new Error(`Failed to fetch stock ${symbol}`)
    }
    return response.json()
  },
  
  async createStock(stock) {
    const response = await fetch(`${API_BASE_URL}/stocks`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(stock)
    })
    if (!response.ok) {
      throw new Error('Failed to create stock')
    }
    return response.json()
  },
  
  async updateStock(symbol, stock) {
    const response = await fetch(`${API_BASE_URL}/stocks/${symbol}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(stock)
    })
    if (!response.ok) {
      throw new Error(`Failed to update stock ${symbol}`)
    }
    return response.json()
  },
  
  async deleteStock(symbol) {
    const response = await fetch(`${API_BASE_URL}/stocks/${symbol}`, {
      method: 'DELETE'
    })
    if (!response.ok) {
      throw new Error(`Failed to delete stock ${symbol}`)
    }
    return response.json()
  },

  async getEquityCurveDaily(symbol, opts = {}) {
    const params = new URLSearchParams()
    if (opts.startDate) params.append('startDate', opts.startDate)
    if (opts.endDate) params.append('endDate', opts.endDate)
    if (typeof opts.compounded === 'boolean') params.append('compounded', String(opts.compounded))
    const qs = params.toString()
    const url = qs
      ? `${API_BASE_URL}/backtests/${symbol}/equity-curve/daily?${qs}`
      : `${API_BASE_URL}/backtests/${symbol}/equity-curve/daily`
    const response = await fetch(url)
    if (!response.ok) {
      throw new Error(`Failed to fetch daily equity curve for ${symbol}`)
    }
    return response.json()
  },

  async getQuote(symbol) {
    const response = await fetch(`${API_BASE_URL}/stocks/${symbol}/quote`)
    if (!response.ok) {
      if (response.status === 429) {
        const data = await response.json()
        throw new Error(`Rate limit exceeded. ${data.message}`)
      }
      throw new Error(`Failed to fetch quote for ${symbol}`)
    }
    return response.json()
  },

  async getHistoricalPrices(symbol, opts = {}) {
    const params = new URLSearchParams()
    if (opts.startDate) params.append('startDate', opts.startDate)
    if (opts.endDate) params.append('endDate', opts.endDate)
    if (opts.interval) params.append('interval', opts.interval)
    const qs = params.toString()
    const url = qs
      ? `${API_BASE_URL}/stocks/${symbol}/historical?${qs}`
      : `${API_BASE_URL}/stocks/${symbol}/historical`
    const response = await fetch(url)
    if (!response.ok) {
      if (response.status === 429) {
        const data = await response.json()
        throw new Error(`Rate limit exceeded. ${data.message}`)
      }
      throw new Error(`Failed to fetch historical prices for ${symbol}`)
    }
    return response.json()
  },

  async getFundamentals(symbol) {
    const response = await fetch(`${API_BASE_URL}/stocks/${symbol}/fundamentals`)
    if (!response.ok) {
      if (response.status === 429) {
        const data = await response.json()
        throw new Error(`Rate limit exceeded. ${data.message}`)
      }
      throw new Error(`Failed to fetch fundamentals for ${symbol}`)
    }
    return response.json()
  },

  async getCompanyProfile(symbol) {
    const response = await fetch(`${API_BASE_URL}/stocks/${symbol}/profile`)
    if (!response.ok) {
      if (response.status === 429) {
        const data = await response.json()
        throw new Error(`Rate limit exceeded. ${data.message}`)
      }
      throw new Error(`Failed to fetch company profile for ${symbol}`)
    }
    return response.json()
  },

  async searchSymbols(query, limit = 10) {
    const params = new URLSearchParams({ query, limit: String(limit) })
    const response = await fetch(`${API_BASE_URL}/stocks/search?${params.toString()}`)
    if (!response.ok) {
      if (response.status === 429) {
        const data = await response.json()
        throw new Error(`Rate limit exceeded. ${data.message}`)
      }
      throw new Error(`Failed to search symbols with query: ${query}`)
    }
    return response.json()
  },

  async getQuotes(symbols) {
    const promises = symbols.map(symbol => this.getQuote(symbol))
    const results = await Promise.allSettled(promises)
    return results
      .filter(result => result.status === 'fulfilled')
      .map(result => result.value)
  }
}

export const aiAgentService = {
  async analyzeStock(symbol) {
    const response = await fetch(`${API_BASE_URL}/agents/analyze`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ symbol })
    })
    if (!response.ok) {
      throw new Error(`Failed to analyze stock ${symbol}`)
    }
    return response.json()
  }
}

export const healthService = {
  async getHealth() {
    const response = await fetch(`${API_BASE_URL}/health`)
    if (!response.ok) {
      throw new Error('Failed to fetch health status')
    }
    return response.json()
  },

  async getMetrics() {
    const response = await fetch(`${API_BASE_URL}/health/metrics`)
    if (!response.ok) {
      throw new Error('Failed to fetch rate limit metrics')
    }
    return response.json()
  }
}

export const backtestService = {
  async runBacktest(payload) {
    const response = await fetch(`${API_BASE_URL}/backtests/run`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    })

    if (!response.ok) {
      const errorText = await response.text()
      throw new Error(errorText || 'Failed to run backtest')
    }

    return response.json()
  },

  async getDashboard(symbol, recent = 10) {
    const response = await fetch(`${API_BASE_URL}/backtests/${symbol}/dashboard?recent=${recent}`)
    if (!response.ok) {
      throw new Error(`Failed to fetch backtest dashboard for ${symbol}`)
    }

    return response.json()
  },

  async getSummary(symbol) {
    const response = await fetch(`${API_BASE_URL}/backtests/${symbol}/summary`)
    if (!response.ok) {
      throw new Error(`Failed to fetch backtest summary for ${symbol}`)
    }

    return response.json()
  },

  async getRecentPerformances(symbol, take = 20) {
    const response = await fetch(`${API_BASE_URL}/backtests/${symbol}/recent?take=${take}`)
    if (!response.ok) {
      throw new Error(`Failed to fetch backtest performances for ${symbol}`)
    }

    return response.json()
  },

  async getEquityCurve(symbol, opts = {}) {
    const params = new URLSearchParams()
    if (opts.startDate) params.append('startDate', opts.startDate)
    if (opts.endDate) params.append('endDate', opts.endDate)
    if (typeof opts.compounded === 'boolean') params.append('compounded', String(opts.compounded))
    const qs = params.toString()
    const url = qs
      ? `${API_BASE_URL}/backtests/${symbol}/equity-curve?${qs}`
      : `${API_BASE_URL}/backtests/${symbol}/equity-curve`
    const response = await fetch(url)
    if (!response.ok) {
      throw new Error(`Failed to fetch equity curve for ${symbol}`)
    }
    return response.json()
  }
}

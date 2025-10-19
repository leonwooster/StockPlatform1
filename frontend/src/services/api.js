const API_BASE_URL = 'https://localhost:5565/api'

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

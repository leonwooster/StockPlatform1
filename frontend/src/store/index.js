import { defineStore } from 'pinia'

export const useStockStore = defineStore('stock', {
  state: () => ({
    stocks: [],
    selectedStock: null,
    loading: false,
    error: null
  }),
  
  actions: {
    async fetchStocks() {
      this.loading = true
      try {
        // In a real app, this would be an API call
        // For now, we'll use mock data
        this.stocks = [
          { symbol: 'AAPL', name: 'Apple Inc.', price: 175.00, change: 1.50 },
          { symbol: 'GOOGL', name: 'Alphabet Inc.', price: 2800.00, change: -10.00 },
          { symbol: 'MSFT', name: 'Microsoft Corp.', price: 330.00, change: 5.25 }
        ]
      } catch (error) {
        this.error = error.message
      } finally {
        this.loading = false
      }
    },
    
    selectStock(stock) {
      this.selectedStock = stock
    }
  },
  
  getters: {
    getStockBySymbol: (state) => (symbol) => {
      return state.stocks.find(stock => stock.symbol === symbol)
    }
  }
})

<template>
  <div class="technical-analysis">
    <h1 class="text-3xl font-bold mb-6">Technical Analysis</h1>
    
    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <div class="flex flex-wrap items-center justify-between mb-4">
        <div class="flex items-center space-x-4 mb-4 md:mb-0">
          <input 
            v-model="selectedSymbol" 
            type="text" 
            placeholder="Enter stock symbol" 
            class="border rounded px-3 py-2"
            @keyup.enter="loadChartData"
          >
          <button 
            @click="loadChartData" 
            class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            Load Chart
          </button>
        </div>
        
        <div class="flex space-x-2">
          <button 
            v-for="period in timePeriods" 
            :key="period" 
            @click="setTimePeriod(period)"
            :class="[
              'px-3 py-1 rounded',
              currentTimePeriod === period 
                ? 'bg-blue-600 text-white' 
                : 'bg-gray-200 hover:bg-gray-300'
            ]"
          >
            {{ period }}
          </button>
        </div>
      </div>
      
      <div class="chart-container mb-6">
        <div class="chart-placeholder h-96 bg-gray-100 rounded flex items-center justify-center">
          <p>Technical chart will be displayed here using ECharts</p>
        </div>
      </div>
      
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-gray-50 p-4 rounded">
          <h3 class="font-semibold mb-2">Indicators</h3>
          <div class="space-y-2">
            <label class="flex items-center">
              <input type="checkbox" class="mr-2" checked> RSI
            </label>
            <label class="flex items-center">
              <input type="checkbox" class="mr-2" checked> MACD
            </label>
            <label class="flex items-center">
              <input type="checkbox" class="mr-2"> Bollinger Bands
            </label>
            <label class="flex items-center">
              <input type="checkbox" class="mr-2"> Moving Average
            </label>
          </div>
        </div>
        
        <div class="bg-gray-50 p-4 rounded">
          <h3 class="font-semibold mb-2">Signal Strength</h3>
          <div class="flex items-center">
            <div class="w-full bg-gray-200 rounded-full h-4">
              <div class="bg-green-600 h-4 rounded-full" style="width: 72%"></div>
            </div>
            <span class="ml-2 font-semibold">72%</span>
          </div>
          <p class="mt-2 text-sm">Strong Buy Signal</p>
        </div>
        
        <div class="bg-gray-50 p-4 rounded">
          <h3 class="font-semibold mb-2">Price Targets</h3>
          <div class="space-y-1">
            <div class="flex justify-between">
              <span>Resistance 1:</span>
              <span class="font-medium">$180.50</span>
            </div>
            <div class="flex justify-between">
              <span>Current:</span>
              <span class="font-medium">$175.25</span>
            </div>
            <div class="flex justify-between">
              <span>Support 1:</span>
              <span class="font-medium">$170.75</span>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="bg-white rounded-lg shadow p-6">
        <h2 class="text-xl font-semibold mb-4">RSI Indicator</h2>
        <div class="chart-placeholder h-64 bg-gray-100 rounded flex items-center justify-center">
          <p>RSI chart will be displayed here</p>
        </div>
        <div class="mt-4">
          <p class="text-center font-semibold">RSI Value: 62.5</p>
          <p class="text-center text-sm text-gray-600">Neutral - No strong signal</p>
        </div>
      </div>
      
      <div class="bg-white rounded-lg shadow p-6">
        <h2 class="text-xl font-semibold mb-4">MACD Indicator</h2>
        <div class="chart-placeholder h-64 bg-gray-100 rounded flex items-center justify-center">
          <p>MACD chart will be displayed here</p>
        </div>
        <div class="mt-4">
          <p class="text-center font-semibold">MACD: 1.25 | Signal: 0.85</p>
          <p class="text-center text-sm text-green-600">Bullish crossover detected</p>
        </div>
      </div>
    </div>
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
      // In a real app, this would load chart data for the selected symbol
      console.log(`Loading chart data for ${this.selectedSymbol}`)
    },
    setTimePeriod(period) {
      this.currentTimePeriod = period
      this.loadChartData()
    }
  }
}
</script>

<style scoped>
.chart-placeholder {
  background: linear-gradient(45deg, #f0f0f0 25%, transparent 25%),
              linear-gradient(-45deg, #f0f0f0 25%, transparent 25%),
              linear-gradient(45deg, transparent 75%, #f0f0f0 75%),
              linear-gradient(-45deg, transparent 75%, #f0f0f0 75%);
  background-size: 20px 20px;
  background-position: 0 0, 0 10px, 10px -10px, -10px 0px;
}
</style>

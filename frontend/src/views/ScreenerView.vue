<template>
  <div class="screener">
    <h1 class="text-3xl font-bold mb-6">Stock Screener</h1>
    
    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <h2 class="text-xl font-semibold mb-4">Screening Criteria</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div>
          <label class="block text-sm font-medium mb-1">Market Cap</label>
          <select class="w-full border rounded px-3 py-2">
            <option>All</option>
            <option>Mega ($200B+)</option>
            <option>Large ($10B-$200B)</option>
            <option>Mid ($2B-$10B)</option>
            <option>Small ($300M-$2B)</option>
          </select>
        </div>
        
        <div>
          <label class="block text-sm font-medium mb-1">Price Range</label>
          <div class="flex space-x-2">
            <input type="number" placeholder="Min" class="w-full border rounded px-3 py-2">
            <input type="number" placeholder="Max" class="w-full border rounded px-3 py-2">
          </div>
        </div>
        
        <div>
          <label class="block text-sm font-medium mb-1">Sector</label>
          <select class="w-full border rounded px-3 py-2">
            <option>All</option>
            <option>Technology</option>
            <option>Healthcare</option>
            <option>Financial</option>
            <option>Consumer</option>
          </select>
        </div>
        
        <div>
          <label class="block text-sm font-medium mb-1">Performance</label>
          <select class="w-full border rounded px-3 py-2">
            <option>Any</option>
            <option>Up 10%+</option>
            <option>Up 20%+</option>
            <option>Down 10%+</option>
            <option>Down 20%+</option>
          </select>
        </div>
      </div>
      
      <div class="flex flex-wrap gap-4 mb-4">
        <button class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
          Run Screen
        </button>
        <button class="border border-gray-300 px-4 py-2 rounded hover:bg-gray-100">
          Save Screen
        </button>
        <button class="border border-gray-300 px-4 py-2 rounded hover:bg-gray-100">
          Load Screen
        </button>
      </div>
    </div>
    
    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <h2 class="text-xl font-semibold mb-4">Intrinsic Value Calculator</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <h3 class="font-medium mb-3">Company Financials</h3>
          <div class="space-y-3">
            <div>
              <label class="block text-sm mb-1">Current Price</label>
              <input type="number" value="175.00" class="w-full border rounded px-3 py-2">
            </div>
            <div>
              <label class="block text-sm mb-1">EPS (TTM)</label>
              <input type="number" value="6.15" class="w-full border rounded px-3 py-2">
            </div>
            <div>
              <label class="block text-sm mb-1">Expected Growth Rate (%)</label>
              <input type="number" value="8.5" class="w-full border rounded px-3 py-2">
            </div>
            <div>
              <label class="block text-sm mb-1">Discount Rate (%)</label>
              <input type="number" value="10" class="w-full border rounded px-3 py-2">
            </div>
          </div>
        </div>
        
        <div>
          <h3 class="font-medium mb-3">Valuation Results</h3>
          <div class="bg-blue-50 p-4 rounded mb-4">
            <div class="flex justify-between mb-2">
              <span>Intrinsic Value:</span>
              <span class="font-semibold">$215.75</span>
            </div>
            <div class="flex justify-between mb-2">
              <span>Margin of Safety:</span>
              <span class="font-semibold text-green-600">23.3%</span>
            </div>
            <div class="flex justify-between">
              <span>Valuation:</span>
              <span class="font-semibold text-green-600">Undervalued</span>
            </div>
          </div>
          <p class="text-sm text-gray-600">
            Based on the DCF model, this stock appears to be undervalued by 23.3%.
            This suggests a potential buying opportunity.
          </p>
        </div>
      </div>
    </div>
    
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-xl font-semibold mb-4">Screen Results (24 stocks)</h2>
      
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Symbol</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Company</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Price</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Change</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Market Cap</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">P/E</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap font-medium">NVDA</td>
              <td class="px-6 py-4 whitespace-nowrap">NVIDIA Corp</td>
              <td class="px-6 py-4 whitespace-nowrap">$425.60</td>
              <td class="px-6 py-4 whitespace-nowrap text-green-600">+3.25%</td>
              <td class="px-6 py-4 whitespace-nowrap">$1.06T</td>
              <td class="px-6 py-4 whitespace-nowrap">85.42</td>
              <td class="px-6 py-4 whitespace-nowrap">
                <button class="text-blue-600 hover:text-blue-900">Analyze</button>
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap font-medium">AMD</td>
              <td class="px-6 py-4 whitespace-nowrap">Advanced Micro Devices</td>
              <td class="px-6 py-4 whitespace-nowrap">$115.30</td>
              <td class="px-6 py-4 whitespace-nowrap text-green-600">+1.80%</td>
              <td class="px-6 py-4 whitespace-nowrap">$142B</td>
              <td class="px-6 py-4 whitespace-nowrap">210.5</td>
              <td class="px-6 py-4 whitespace-nowrap">
                <button class="text-blue-600 hover:text-blue-900">Analyze</button>
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap font-medium">TSLA</td>
              <td class="px-6 py-4 whitespace-nowrap">Tesla Inc</td>
              <td class="px-6 py-4 whitespace-nowrap">$245.75</td>
              <td class="px-6 py-4 whitespace-nowrap text-red-600">-0.95%</td>
              <td class="px-6 py-4 whitespace-nowrap">$780B</td>
              <td class="px-6 py-4 whitespace-nowrap">65.2</td>
              <td class="px-6 py-4 whitespace-nowrap">
                <button class="text-blue-600 hover:text-blue-900">Analyze</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <div class="mt-4 flex justify-between items-center">
        <div class="text-sm text-gray-700">
          Showing <span class="font-medium">1</span> to <span class="font-medium">3</span> of <span class="font-medium">24</span> results
        </div>
        <div class="flex space-x-2">
          <button class="px-3 py-1 border rounded hover:bg-gray-100">Previous</button>
          <button class="px-3 py-1 border rounded hover:bg-gray-100">Next</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ScreenerView'
}
</script>

<style scoped>
/* Add any component-specific styles here */
</style>

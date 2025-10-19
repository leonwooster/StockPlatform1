<template>
  <div class="news">
    <h1 class="text-3xl font-bold mb-6">Market News & Sentiment</h1>
    
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
      <div class="lg:col-span-2 bg-white rounded-lg shadow p-6">
        <h2 class="text-xl font-semibold mb-4">Latest News</h2>
        
        <div class="space-y-4">
          <div class="border-b pb-4">
            <h3 class="font-medium text-lg mb-1">Fed Policy Meeting Tomorrow: What to Expect</h3>
            <p class="text-gray-600 text-sm mb-2">Bloomberg • 2 hours ago</p>
            <p class="mb-2">The Federal Reserve's upcoming policy meeting is expected to provide insights into future interest rate decisions...</p>
            <div class="flex items-center text-sm text-gray-500">
              <span class="mr-4">📈 Sentiment: Positive</span>
              <span>💼 Sector: Financial</span>
            </div>
          </div>
          
          <div class="border-b pb-4">
            <h3 class="font-medium text-lg mb-1">Tech Stocks Rally on Strong Earnings Reports</h3>
            <p class="text-gray-600 text-sm mb-2">Reuters • 4 hours ago</p>
            <p class="mb-2">Major technology companies posted better-than-expected quarterly earnings, driving a market rally...</p>
            <div class="flex items-center text-sm text-gray-500">
              <span class="mr-4">📈 Sentiment: Very Positive</span>
              <span>💼 Sector: Technology</span>
            </div>
          </div>
          
          <div class="border-b pb-4">
            <h3 class="font-medium text-lg mb-1">Oil Prices Surge Amid Supply Concerns</h3>
            <p class="text-gray-600 text-sm mb-2">CNBC • 6 hours ago</p>
            <p class="mb-2">Crude oil prices jumped after OPEC+ announced deeper production cuts...</p>
            <div class="flex items-center text-sm text-gray-500">
              <span class="mr-4">📈 Sentiment: Neutral</span>
              <span>💼 Sector: Energy</span>
            </div>
          </div>
        </div>
        
        <div class="mt-4 text-center">
          <button class="text-blue-600 hover:text-blue-800 font-medium">
            Load More News
          </button>
        </div>
      </div>
      
      <div class="space-y-6">
        <div class="bg-white rounded-lg shadow p-6">
          <h2 class="text-xl font-semibold mb-4">Market Sentiment</h2>
          
          <div class="space-y-4">
            <div>
              <div class="flex justify-between mb-1">
                <span>Bullish</span>
                <span>62%</span>
              </div>
              <div class="w-full bg-gray-200 rounded-full h-2">
                <div class="bg-green-600 h-2 rounded-full" style="width: 62%"></div>
              </div>
            </div>
            
            <div>
              <div class="flex justify-between mb-1">
                <span>Neutral</span>
                <span>25%</span>
              </div>
              <div class="w-full bg-gray-200 rounded-full h-2">
                <div class="bg-yellow-500 h-2 rounded-full" style="width: 25%"></div>
              </div>
            </div>
            
            <div>
              <div class="flex justify-between mb-1">
                <span>Bearish</span>
                <span>13%</span>
              </div>
              <div class="w-full bg-gray-200 rounded-full h-2">
                <div class="bg-red-600 h-2 rounded-full" style="width: 13%"></div>
              </div>
            </div>
          </div>
          
          <div class="mt-4 pt-4 border-t">
            <p class="text-center font-medium">Overall Market Sentiment: Bullish</p>
          </div>
        </div>
        
        <div class="bg-white rounded-lg shadow p-6">
          <h2 class="text-xl font-semibold mb-4">Trending Topics</h2>
          
          <div class="space-y-3">
            <div class="flex items-center p-2 hover:bg-gray-50 rounded cursor-pointer">
              <span class="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded mr-2">1</span>
              <span>Fed Meeting</span>
            </div>
            <div class="flex items-center p-2 hover:bg-gray-50 rounded cursor-pointer">
              <span class="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded mr-2">2</span>
              <span>Tech Earnings</span>
            </div>
            <div class="flex items-center p-2 hover:bg-gray-50 rounded cursor-pointer">
              <span class="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded mr-2">3</span>
              <span>OPEC+ Cuts</span>
            </div>
            <div class="flex items-center p-2 hover:bg-gray-50 rounded cursor-pointer">
              <span class="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded mr-2">4</span>
              <span>AI Developments</span>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <h2 class="text-xl font-semibold mb-4">Social Media Mentions</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="border rounded p-4">
          <h3 class="font-medium mb-2">Reddit (r/wallstreetbets)</h3>
          <div class="flex items-center mb-2">
            <span class="text-2xl font-bold mr-2">1,245</span>
            <span class="text-sm">mentions today</span>
          </div>
          <div class="text-green-600">↑ 15% from yesterday</div>
        </div>
        
        <div class="border rounded p-4">
          <h3 class="font-medium mb-2">Twitter</h3>
          <div class="flex items-center mb-2">
            <span class="text-2xl font-bold mr-2">8,762</span>
            <span class="text-sm">mentions today</span>
          </div>
          <div class="text-green-600">↑ 8% from yesterday</div>
        </div>
        
        <div class="border rounded p-4">
          <h3 class="font-medium mb-2">StockTwits</h3>
          <div class="flex items-center mb-2">
            <span class="text-2xl font-bold mr-2">3,421</span>
            <span class="text-sm">mentions today</span>
          </div>
          <div class="text-red-600">↓ 3% from yesterday</div>
        </div>
      </div>
    </div>
    
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-xl font-semibold mb-4">Earnings Calendar</h2>
      
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Symbol</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Company</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">EPS Estimate</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Revenue Estimate</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap">Oct 19, 2025</td>
              <td class="px-6 py-4 whitespace-nowrap font-medium">AAPL</td>
              <td class="px-6 py-4 whitespace-nowrap">Apple Inc.</td>
              <td class="px-6 py-4 whitespace-nowrap">$1.29</td>
              <td class="px-6 py-4 whitespace-nowrap">$89.5B</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap">Oct 20, 2025</td>
              <td class="px-6 py-4 whitespace-nowrap font-medium">GOOGL</td>
              <td class="px-6 py-4 whitespace-nowrap">Alphabet Inc.</td>
              <td class="px-6 py-4 whitespace-nowrap">$1.52</td>
              <td class="px-6 py-4 whitespace-nowrap">$78.2B</td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap">Oct 21, 2025</td>
              <td class="px-6 py-4 whitespace-nowrap font-medium">AMZN</td>
              <td class="px-6 py-4 whitespace-nowrap">Amazon.com Inc.</td>
              <td class="px-6 py-4 whitespace-nowrap">$0.98</td>
              <td class="px-6 py-4 whitespace-nowrap">$145.0B</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'NewsView'
}
</script>

<style scoped>
/* Add any component-specific styles here */
</style>

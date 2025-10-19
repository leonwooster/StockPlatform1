<template>
  <div class="ai-agents">
    <h1 class="text-3xl font-bold mb-6">AI Trading Agents</h1>
    
    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <div class="flex flex-wrap items-center justify-between mb-4">
        <div class="flex items-center space-x-4 mb-4 md:mb-0">
          <input 
            v-model="selectedSymbol" 
            type="text" 
            placeholder="Enter stock symbol" 
            class="border rounded px-3 py-2"
            @keyup.enter="analyzeStock"
          >
          <button 
            @click="analyzeStock" 
            class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
            :disabled="analyzing"
          >
            {{ analyzing ? 'Analyzing...' : 'Analyze Stock' }}
          </button>
        </div>
        
        <div class="flex space-x-2">
          <button 
            class="px-3 py-1 rounded border hover:bg-gray-100"
            @click="toggleAgent('fundamental')"
            :class="enabledAgents.fundamental ? 'bg-blue-100 border-blue-300' : ''"
          >
            Fundamental
          </button>
          <button 
            class="px-3 py-1 rounded border hover:bg-gray-100"
            @click="toggleAgent('technical')"
            :class="enabledAgents.technical ? 'bg-blue-100 border-blue-300' : ''"
          >
            Technical
          </button>
          <button 
            class="px-3 py-1 rounded border hover:bg-gray-100"
            @click="toggleAgent('sentiment')"
            :class="enabledAgents.sentiment ? 'bg-blue-100 border-blue-300' : ''"
          >
            Sentiment
          </button>
        </div>
      </div>
      
      <div v-if="analysisResult" class="mt-6">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
          <div class="lg:col-span-2">
            <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
              <h2 class="text-xl font-semibold mb-3">Trading Signal</h2>
              <div class="flex items-center">
                <div class="mr-4">
                  <span 
                    class="text-2xl font-bold"
                    :class="{
                      'text-green-600': analysisResult.signal.type === 'Buy' || analysisResult.signal.type === 'StrongBuy',
                      'text-red-600': analysisResult.signal.type === 'Sell' || analysisResult.signal.type === 'StrongSell',
                      'text-yellow-600': analysisResult.signal.type === 'Hold'
                    }"
                  >
                    {{ analysisResult.signal.type }}
                  </span>
                </div>
                <div class="flex-1">
                  <div class="flex items-center mb-1">
                    <span class="mr-2">Confidence:</span>
                    <div class="w-full bg-gray-200 rounded-full h-2">
                      <div 
                        class="h-2 rounded-full"
                        :class="{
                          'bg-green-600': analysisResult.signal.confidence >= 70,
                          'bg-yellow-500': analysisResult.signal.confidence >= 40 && analysisResult.signal.confidence < 70,
                          'bg-red-600': analysisResult.signal.confidence < 40
                        }"
                        :style="`width: ${analysisResult.signal.confidence}%`"
                      ></div>
                    </div>
                    <span class="ml-2 font-medium">{{ analysisResult.signal.confidence }}%</span>
                  </div>
                  <p class="text-sm">{{ analysisResult.signal.rationale }}</p>
                </div>
              </div>
            </div>
            
            <div class="bg-white border rounded-lg p-4 mb-6">
              <h2 class="text-xl font-semibold mb-3">Agent Analyses</h2>
              
              <div class="space-y-4">
                <div 
                  v-for="analysis in analysisResult.analyses" 
                  :key="analysis.agentType"
                  class="border rounded p-4"
                >
                  <div class="flex justify-between items-center mb-2">
                    <h3 class="font-medium">
                      {{ getAgentName(analysis.agentType) }}
                    </h3>
                    <span 
                      class="text-sm px-2 py-1 rounded"
                      :class="{
                        'bg-green-100 text-green-800': analysis.confidenceScore >= 70,
                        'bg-yellow-100 text-yellow-800': analysis.confidenceScore >= 40 && analysis.confidenceScore < 70,
                        'bg-red-100 text-red-800': analysis.confidenceScore < 40
                      }"
                    >
                      Confidence: {{ analysis.confidenceScore }}%
                    </span>
                  </div>
                  <p class="text-gray-700">{{ analysis.analysis }}</p>
                </div>
              </div>
            </div>
          </div>
          
          <div>
            <div class="bg-white border rounded-lg p-4 mb-6">
              <h2 class="text-xl font-semibold mb-3">Risk Assessment</h2>
              
              <div class="space-y-4">
                <div>
                  <div class="flex justify-between mb-1">
                    <span>Risk Score</span>
                    <span class="font-medium">{{ analysisResult.riskAssessment.riskScore }}/100</span>
                  </div>
                  <div class="w-full bg-gray-200 rounded-full h-2">
                    <div 
                      class="h-2 rounded-full"
                      :class="{
                        'bg-green-600': analysisResult.riskAssessment.riskScore <= 30,
                        'bg-yellow-500': analysisResult.riskAssessment.riskScore > 30 && analysisResult.riskAssessment.riskScore <= 70,
                        'bg-red-600': analysisResult.riskAssessment.riskScore > 70
                      }"
                      :style="`width: ${analysisResult.riskAssessment.riskScore}%`"
                    ></div>
                  </div>
                  <p class="text-sm mt-1">{{ analysisResult.riskAssessment.riskLevel }} risk</p>
                </div>
                
                <div>
                  <p class="flex justify-between">
                    <span>Recommended Position Size:</span>
                    <span class="font-medium">{{ analysisResult.riskAssessment.recommendedPositionSize }}%</span>
                  </p>
                  <p class="flex justify-between">
                    <span>Suggested Stop Loss:</span>
                    <span class="font-medium">${{ analysisResult.riskAssessment.suggestedStopLoss }}</span>
                  </p>
                  <p class="flex justify-between">
                    <span>Suggested Take Profit:</span>
                    <span class="font-medium">${{ analysisResult.riskAssessment.suggestedTakeProfit }}</span>
                  </p>
                </div>
                
                <div>
                  <h4 class="font-medium mb-2">Key Risk Factors</h4>
                  <ul class="list-disc list-inside text-sm space-y-1">
                    <li v-for="factor in analysisResult.riskAssessment.riskFactors" :key="factor">
                      {{ factor }}
                    </li>
                  </ul>
                </div>
              </div>
            </div>
            
            <div class="bg-white border rounded-lg p-4">
              <h2 class="text-xl font-semibold mb-3">Agent Debate</h2>
              
              <div class="space-y-4">
                <div>
                  <h3 class="font-medium text-green-600 mb-2">Bullish Arguments</h3>
                  <ul class="list-disc list-inside space-y-2">
                    <li v-for="(arg, index) in analysisResult.debate.bullishArguments" :key="index">
                      <strong>{{ arg.point }}</strong>
                      <p class="text-sm text-gray-600 mt-1">{{ arg.evidence }}</p>
                    </li>
                  </ul>
                </div>
                
                <div>
                  <h3 class="font-medium text-red-600 mb-2">Bearish Arguments</h3>
                  <ul class="list-disc list-inside space-y-2">
                    <li v-for="(arg, index) in analysisResult.debate.bearishArguments" :key="index">
                      <strong>{{ arg.point }}</strong>
                      <p class="text-sm text-gray-600 mt-1">{{ arg.evidence }}</p>
                    </li>
                  </ul>
                </div>
                
                <div class="bg-gray-50 p-3 rounded">
                  <h3 class="font-medium mb-1">Consensus</h3>
                  <p class="text-sm">{{ analysisResult.debate.consensus }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <div v-else class="text-center py-12">
        <div class="text-gray-400 mb-4">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
        </div>
        <p class="text-gray-600">Enter a stock symbol and click "Analyze Stock" to get AI-powered trading insights</p>
      </div>
    </div>
    
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-xl font-semibold mb-4">Agent Performance</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div class="border rounded p-4 text-center">
          <div class="text-2xl font-bold text-blue-600">72%</div>
          <div class="text-sm text-gray-600">Signal Accuracy</div>
        </div>
        <div class="border rounded p-4 text-center">
          <div class="text-2xl font-bold text-green-600">12.4%</div>
          <div class="text-sm text-gray-600">Avg. Return</div>
        </div>
        <div class="border rounded p-4 text-center">
          <div class="text-2xl font-bold text-purple-600">68%</div>
          <div class="text-sm text-gray-600">Win Rate</div>
        </div>
        <div class="border rounded p-4 text-center">
          <div class="text-2xl font-bold text-yellow-600">42</div>
          <div class="text-sm text-gray-600">Signals Generated</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'AIAgentsView',
  data() {
    return {
      selectedSymbol: 'AAPL',
      analyzing: false,
      enabledAgents: {
        fundamental: true,
        technical: true,
        sentiment: true
      },
      analysisResult: null
    }
  },
  methods: {
    async analyzeStock() {
      if (!this.selectedSymbol) return
      
      this.analyzing = true
      
      // In a real app, this would call the AI agent service
      // For now, we'll simulate the response
      try {
        // Simulate API call delay
        await new Promise(resolve => setTimeout(resolve, 1500))
        
        // Mock response
        this.analysisResult = {
          symbol: this.selectedSymbol,
          timestamp: new Date().toISOString(),
          signal: {
            type: 'Buy',
            confidence: 78,
            targetPrice: 185.50,
            stopLoss: 168.20,
            rationale: 'Strong fundamentals combined with positive technical indicators and sentiment suggest upward momentum.'
          },
          analyses: [
            {
              agentType: 'FundamentalAnalyst',
              analysis: 'Company shows strong revenue growth of 8.2% and healthy profit margins of 25.3%. Debt-to-equity ratio is favorable at 0.45.',
              confidenceScore: 85
            },
            {
              agentType: 'TechnicalAnalyst',
              analysis: 'Price is breaking out of resistance level with strong volume. RSI indicates upward momentum without overbought conditions.',
              confidenceScore: 72
            },
            {
              agentType: 'SentimentAnalyst',
              analysis: 'Social media sentiment is overwhelmingly positive with 78% bullish mentions. Recent news coverage is favorable.',
              confidenceScore: 65
            }
          ],
          debate: {
            bullishArguments: [
              {
                point: 'Strong earnings growth trajectory',
                evidence: 'EPS growth of 12% YoY with upward guidance revisions'
              },
              {
                point: 'Technical breakout pattern',
                evidence: 'Price breaking above 50-day moving average with increased volume'
              }
            ],
            bearishArguments: [
              {
                point: 'High valuation concerns',
                evidence: 'P/E ratio of 28.5 is above industry average of 22.3'
              },
              {
                point: 'Market volatility risks',
                evidence: 'Increased market volatility index suggests potential downside'
              }
            ],
            consensus: 'Moderately bullish with strong fundamentals offsetting valuation concerns. Technical breakout provides additional confirmation.'
          },
          riskAssessment: {
            riskScore: 45,
            riskLevel: 'Moderate',
            recommendedPositionSize: 5.0,
            suggestedStopLoss: 168.20,
            suggestedTakeProfit: 192.75,
            riskFactors: [
              'Market volatility',
              'Sector concentration',
              'Valuation concerns'
            ]
          }
        }
      } catch (error) {
        console.error('Analysis failed:', error)
      } finally {
        this.analyzing = false
      }
    },
    toggleAgent(agent) {
      this.enabledAgents[agent] = !this.enabledAgents[agent]
    },
    getAgentName(agentType) {
      const names = {
        'FundamentalAnalyst': 'Fundamental Analyst',
        'TechnicalAnalyst': 'Technical Analyst',
        'SentimentAnalyst': 'Sentiment Analyst',
        'NewsAnalyst': 'News Analyst',
        'BullishResearcher': 'Bullish Researcher',
        'BearishResearcher': 'Bearish Researcher',
        'Trader': 'Trader Agent',
        'RiskManager': 'Risk Manager'
      }
      return names[agentType] || agentType
    }
  }
}
</script>

<style scoped>
/* Add any component-specific styles here */
</style>

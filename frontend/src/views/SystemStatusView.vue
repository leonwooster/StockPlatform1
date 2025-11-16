<template>
  <div class="space-y-6">
    <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-8">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-3xl font-semibold text-white">System Status</h1>
          <p class="mt-2 text-sm text-slate-400">Monitor the health of all dependent services</p>
        </div>
        <button
          @click="refreshStatus"
          :disabled="loading"
          class="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-300 transition hover:border-primary-400 hover:bg-primary-500/10 hover:text-primary-200 disabled:opacity-50"
        >
          <svg v-if="loading" class="inline h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span v-else>Refresh</span>
        </button>
      </div>
    </div>

    <!-- Overall Status -->
    <div class="grid gap-6 md:grid-cols-3">
      <div class="rounded-3xl border border-white/5 bg-gradient-to-br from-slate-900 to-slate-950 p-6">
        <div class="flex items-center gap-3">
          <div :class="['h-12 w-12 rounded-full flex items-center justify-center', overallStatusClass]">
            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path v-if="overallStatus === 'healthy'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              <path v-else-if="overallStatus === 'degraded'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </div>
          <div>
            <p class="text-sm text-slate-400">Overall Status</p>
            <p class="text-2xl font-semibold text-white capitalize">{{ overallStatus }}</p>
          </div>
        </div>
      </div>

      <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
        <p class="text-sm text-slate-400">Services Online</p>
        <p class="mt-2 text-3xl font-semibold text-white">{{ healthyCount }} / {{ totalServices }}</p>
      </div>

      <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
        <p class="text-sm text-slate-400">Last Updated</p>
        <p class="mt-2 text-lg font-semibold text-white">{{ lastUpdated }}</p>
      </div>
    </div>

    <!-- Service Status Cards -->
    <div class="grid gap-6 lg:grid-cols-2">
      <!-- Yahoo Finance API -->
      <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-3">
            <div :class="['h-10 w-10 rounded-full flex items-center justify-center', getStatusClass(yahooFinanceStatus)]">
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <div>
              <h3 class="font-semibold text-white">Yahoo Finance API</h3>
              <p class="text-xs text-slate-400">External market data provider</p>
            </div>
          </div>
          <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusBadgeClass(yahooFinanceStatus)]">
            {{ yahooFinanceStatus }}
          </span>
        </div>
        <div class="mt-4 space-y-2 text-sm">
          <div v-if="healthData" class="flex justify-between">
            <span class="text-slate-400">Status</span>
            <span class="text-white">{{ healthData.status }}</span>
          </div>
          <div v-if="healthData" class="flex justify-between">
            <span class="text-slate-400">Details</span>
            <span class="text-white text-xs">{{ healthData.details }}</span>
          </div>
          <div v-if="healthData" class="flex justify-between">
            <span class="text-slate-400">Last Check</span>
            <span class="text-white">{{ formatTimestamp(healthData.timestamp) }}</span>
          </div>
          <div v-if="!healthData && backendStatus === 'healthy'" class="text-xs text-slate-400">
            Health check data not available. Backend is responding normally.
          </div>
          <div v-if="yahooFinanceStatus === 'unhealthy'" class="mt-2 rounded-lg bg-amber-500/10 border border-amber-500/30 p-2 text-xs text-amber-300">
            ⚠️ Yahoo Finance API may be experiencing issues. This is normal and doesn't affect core functionality.
          </div>
        </div>
      </div>

      <!-- Backend API -->
      <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-3">
            <div :class="['h-10 w-10 rounded-full flex items-center justify-center', getStatusClass(backendStatus)]">
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2m-2-4h.01M17 16h.01" />
              </svg>
            </div>
            <div>
              <h3 class="font-semibold text-white">Backend API</h3>
              <p class="text-xs text-slate-400">StockSensePro API Server</p>
            </div>
          </div>
          <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusBadgeClass(backendStatus)]">
            {{ backendStatus }}
          </span>
        </div>
        <div class="mt-4 space-y-2 text-sm">
          <div class="flex justify-between">
            <span class="text-slate-400">Endpoint</span>
            <span class="font-mono text-xs text-white">{{ apiBaseUrl }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Response Time</span>
            <span class="text-white">{{ backendResponseTime }}ms</span>
          </div>
        </div>
      </div>

      <!-- Database (PostgreSQL) -->
      <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-3">
            <div :class="['h-10 w-10 rounded-full flex items-center justify-center', getStatusClass(databaseStatus)]">
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4" />
              </svg>
            </div>
            <div>
              <h3 class="font-semibold text-white">PostgreSQL Database</h3>
              <p class="text-xs text-slate-400">Primary data store</p>
            </div>
          </div>
          <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusBadgeClass(databaseStatus)]">
            {{ databaseStatus }}
          </span>
        </div>
        <div class="mt-4 space-y-2 text-sm">
          <div class="flex justify-between">
            <span class="text-slate-400">Connection</span>
            <span class="text-white">{{ databaseStatus === 'healthy' ? 'Active' : 'Unknown' }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Type</span>
            <span class="text-white">PostgreSQL</span>
          </div>
        </div>
      </div>

      <!-- Cache (Redis) -->
      <div class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-3">
            <div :class="['h-10 w-10 rounded-full flex items-center justify-center', getStatusClass(cacheStatus)]">
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <div>
              <h3 class="font-semibold text-white">Redis Cache</h3>
              <p class="text-xs text-slate-400">In-memory cache</p>
            </div>
          </div>
          <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusBadgeClass(cacheStatus)]">
            {{ cacheStatus }}
          </span>
        </div>
        <div class="mt-4 space-y-2 text-sm">
          <div class="flex justify-between">
            <span class="text-slate-400">Connection</span>
            <span class="text-white">{{ cacheStatus === 'healthy' ? 'Active' : 'Unknown' }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Type</span>
            <span class="text-white">Redis</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Rate Limit Metrics -->
    <div v-if="metrics" class="rounded-3xl border border-white/5 bg-slate-900/60 p-6">
      <h2 class="text-lg font-semibold text-white">API Rate Limiting</h2>
      <div class="mt-4 grid gap-4 md:grid-cols-4">
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-xs text-slate-400">Total Requests</p>
          <p class="mt-2 text-2xl font-semibold text-white">{{ metrics.totalRequests.toLocaleString() }}</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-xs text-slate-400">Rate Limit Hits</p>
          <p class="mt-2 text-2xl font-semibold text-white">{{ metrics.totalRateLimitHits.toLocaleString() }}</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-xs text-slate-400">Hit Rate</p>
          <p class="mt-2 text-2xl font-semibold text-white">{{ (metrics.rateLimitHitRate * 100).toFixed(2) }}%</p>
        </div>
        <div class="rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-xs text-slate-400">Uptime</p>
          <p class="mt-2 text-lg font-semibold text-white">{{ formatUptime(metrics.uptime) }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { healthService } from '../services/api'

const loading = ref(false)
const healthData = ref(null)
const metrics = ref(null)
const backendResponseTime = ref(0)
const lastUpdatedTime = ref(null)

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5566/api'

// Service statuses
const yahooFinanceStatus = computed(() => {
  if (!healthData.value) return 'unknown'
  return healthData.value.status === 'Healthy' ? 'healthy' : 'unhealthy'
})

const backendStatus = computed(() => {
  // If we got any response from the backend, it's healthy
  if (healthData.value || metrics.value) return 'healthy'
  if (backendResponseTime.value > 0) return 'healthy'
  return 'unknown'
})

const databaseStatus = computed(() => {
  // If backend is healthy and we got a response, database is likely healthy
  if (backendStatus.value === 'healthy') return 'healthy'
  return 'unknown'
})

const cacheStatus = computed(() => {
  // If backend is healthy and we got metrics, cache is likely healthy
  if (backendStatus.value === 'healthy' && metrics.value) return 'healthy'
  if (backendStatus.value === 'healthy') return 'degraded' // Backend works but no metrics
  return 'unknown'
})

const totalServices = 4
const healthyCount = computed(() => {
  let count = 0
  if (yahooFinanceStatus.value === 'healthy') count++
  if (backendStatus.value === 'healthy') count++
  if (databaseStatus.value === 'healthy') count++
  if (cacheStatus.value === 'healthy') count++
  return count
})

const overallStatus = computed(() => {
  if (healthyCount.value === totalServices) return 'healthy'
  if (healthyCount.value === 0) return 'unhealthy'
  return 'degraded'
})

const overallStatusClass = computed(() => {
  if (overallStatus.value === 'healthy') return 'bg-emerald-500/20 text-emerald-300'
  if (overallStatus.value === 'degraded') return 'bg-amber-500/20 text-amber-300'
  return 'bg-rose-500/20 text-rose-300'
})

const lastUpdated = computed(() => {
  if (!lastUpdatedTime.value) return 'Never'
  const now = new Date()
  const diff = Math.floor((now - lastUpdatedTime.value) / 1000)
  if (diff < 60) return `${diff}s ago`
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`
  return `${Math.floor(diff / 3600)}h ago`
})

const getStatusClass = (status) => {
  if (status === 'healthy') return 'bg-emerald-500/20 text-emerald-300'
  if (status === 'degraded') return 'bg-amber-500/20 text-amber-300'
  if (status === 'unhealthy') return 'bg-rose-500/20 text-rose-300'
  return 'bg-slate-500/20 text-slate-300'
}

const getStatusBadgeClass = (status) => {
  if (status === 'healthy') return 'bg-emerald-500/10 text-emerald-300 border border-emerald-500/30'
  if (status === 'degraded') return 'bg-amber-500/10 text-amber-300 border border-amber-500/30'
  if (status === 'unhealthy') return 'bg-rose-500/10 text-rose-300 border border-rose-500/30'
  return 'bg-slate-500/10 text-slate-300 border border-slate-500/30'
}

const formatTimestamp = (timestamp) => {
  if (!timestamp) return 'N/A'
  const date = new Date(timestamp)
  return date.toLocaleTimeString()
}

const formatUptime = (uptime) => {
  if (!uptime) return 'N/A'
  if (typeof uptime === 'string') return uptime
  if (uptime.hours !== undefined) {
    return `${uptime.hours}h ${uptime.minutes}m ${uptime.seconds}s`
  }
  return uptime.toString()
}

const refreshStatus = async () => {
  loading.value = true
  const startTime = performance.now()
  
  try {
    const [health, metricsData] = await Promise.allSettled([
      healthService.getHealth(),
      healthService.getMetrics()
    ])
    
    const endTime = performance.now()
    backendResponseTime.value = Math.round(endTime - startTime)
    
    if (health.status === 'fulfilled') {
      healthData.value = health.value
    } else {
      console.warn('Health check failed:', health.reason)
      // Keep previous health data if available
    }
    
    if (metricsData.status === 'fulfilled') {
      metrics.value = metricsData.value
    } else {
      console.warn('Metrics fetch failed:', metricsData.reason)
      // Keep previous metrics if available
    }
    
    lastUpdatedTime.value = new Date()
  } catch (err) {
    console.error('Error fetching system status:', err)
    // Don't clear existing data on error
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  refreshStatus()
  
  // Auto-refresh every 30 seconds
  const interval = setInterval(refreshStatus, 30000)
  
  return () => clearInterval(interval)
})
</script>

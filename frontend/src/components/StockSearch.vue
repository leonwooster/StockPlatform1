<template>
  <div class="relative">
    <div class="relative">
      <input
        v-model="searchQuery"
        @input="handleSearch"
        @focus="showResults = true"
        type="text"
        placeholder="Search stocks by symbol or name..."
        class="w-full rounded-2xl border border-white/10 bg-slate-900/60 px-4 py-3 pl-10 text-sm text-white placeholder-slate-400 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-400/20"
      />
      <svg class="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <div v-if="loading" class="absolute right-3 top-1/2 -translate-y-1/2">
        <svg class="h-5 w-5 animate-spin text-primary-400" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      </div>
    </div>

    <!-- Search Results Dropdown -->
    <div
      v-if="showResults && (searchResults.length > 0 || error)"
      class="absolute z-10 mt-2 w-full rounded-2xl border border-white/10 bg-slate-900 shadow-xl"
    >
      <div v-if="error" class="p-4 text-sm text-rose-300">
        {{ error }}
      </div>
      <ul v-else class="max-h-96 overflow-y-auto">
        <li
          v-for="result in searchResults"
          :key="result.symbol"
          @click="selectStock(result)"
          class="cursor-pointer border-b border-white/5 px-4 py-3 transition hover:bg-white/5 last:border-b-0"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="font-medium text-white">{{ result.symbol }}</p>
              <p class="text-xs text-slate-400">{{ result.name }}</p>
            </div>
            <div class="text-right text-xs text-slate-400">
              <p>{{ result.exchange }}</p>
              <p>{{ result.assetType }}</p>
            </div>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { stockService } from '../services/api'

const emit = defineEmits(['select'])

const searchQuery = ref('')
const searchResults = ref([])
const loading = ref(false)
const error = ref(null)
const showResults = ref(false)
let searchTimeout = null

const handleSearch = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }

  if (searchQuery.value.trim().length < 1) {
    searchResults.value = []
    return
  }

  searchTimeout = setTimeout(async () => {
    loading.value = true
    error.value = null
    try {
      const results = await stockService.searchSymbols(searchQuery.value.trim())
      searchResults.value = results
    } catch (err) {
      error.value = err.message
      searchResults.value = []
    } finally {
      loading.value = false
    }
  }, 300) // Debounce for 300ms
}

const selectStock = (stock) => {
  emit('select', stock)
  searchQuery.value = stock.symbol
  showResults.value = false
}

// Close dropdown when clicking outside
if (typeof window !== 'undefined') {
  document.addEventListener('click', (e) => {
    if (!e.target.closest('.relative')) {
      showResults.value = false
    }
  })
}
</script>

<template>
  <div class="min-h-screen bg-slate-950 text-slate-100">
    <div class="relative">
      <div class="pointer-events-none absolute inset-0 -z-10 bg-[radial-gradient(circle_at_top,_rgba(72,127,255,0.25),_transparent_60%)]"></div>

      <header class="sticky top-0 z-40 border-b border-white/10 bg-slate-950/70 backdrop-blur">
        <div class="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
          <div class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-full bg-primary-500/20 text-primary-300">
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path stroke-linecap="round" stroke-linejoin="round" d="M4 17l4-4 3 3 7-8" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 5h18" />
              </svg>
            </span>
            <div>
              <h1 class="text-lg font-semibold tracking-tight">StockSense Pro</h1>
              <p class="text-xs text-slate-400">AI-first trading intelligence platform</p>
            </div>
          </div>
          <nav class="hidden gap-2 md:flex">
            <RouterLink
              v-for="link in links"
              :key="link.to"
              :to="link.to"
              class="rounded-full px-4 py-2 text-sm font-medium transition"
              :class="isActive(link.to)
                ? 'bg-primary-500 text-white shadow-soft'
                : 'text-slate-300 hover:bg-white/10 hover:text-white'"
            >
              {{ link.label }}
            </RouterLink>
          </nav>
          <button
            class="flex h-10 w-10 items-center justify-center rounded-full border border-white/10 text-slate-300 transition hover:border-primary-400 hover:text-primary-200 md:hidden"
            @click="toggleMenu"
          >
            <i class="i-mdi-menu text-xl"></i>
          </button>
        </div>

        <transition name="fade">
          <nav
            v-if="mobileMenu"
            class="border-t border-white/10 bg-slate-950/90 px-6 pb-6 pt-4 md:hidden"
          >
            <RouterLink
              v-for="link in links"
              :key="link.to"
              :to="link.to"
              class="block rounded-xl px-4 py-2 text-sm font-medium transition"
              :class="isActive(link.to)
                ? 'bg-primary-500 text-white shadow-soft'
                : 'text-slate-300 hover:bg-white/10 hover:text-white'"
              @click="mobileMenu = false"
            >
              {{ link.label }}
            </RouterLink>
          </nav>
        </transition>
      </header>
    </div>

    <main class="mx-auto max-w-7xl px-6 py-8">
      <RouterView />
    </main>

    <footer class="border-t border-white/10 bg-slate-950/80">
      <div class="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-6 py-6 text-sm text-slate-400 md:flex-row">
        <p>© {{ new Date().getFullYear() }} StockSense Pro. Crafted for insight-driven investors.</p>
        <div class="flex items-center gap-3">
          <a href="#" class="rounded-full border border-white/10 px-3 py-1 transition hover:border-primary-400 hover:text-primary-200">Release Notes</a>
          <a href="#" class="rounded-full border border-white/10 px-3 py-1 transition hover:border-primary-400 hover:text-primary-200">Support</a>
          <a href="#" class="rounded-full border border-white/10 px-3 py-1 transition hover:border-primary-400 hover:text-primary-200">Status</a>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'

const links = [
  { label: 'Dashboard', to: '/' },
  { label: 'Technical', to: '/technical' },
  { label: 'Screener', to: '/screener' },
  { label: 'News & Sentiment', to: '/news' },
  { label: 'AI Agents', to: '/ai-agents' }
]

const route = useRoute()
const mobileMenu = ref(false)

const isActive = (path) => {
  if (path === '/') {
    return route.path === '/'
  }
  return route.path.startsWith(path)
}
const toggleMenu = () => {
  mobileMenu.value = !mobileMenu.value
}
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>

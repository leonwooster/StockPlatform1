import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'Dashboard',
    component: () => import('../views/DashboardView.vue')
  },
  {
    path: '/technical',
    name: 'Technical',
    component: () => import('../views/TechnicalView.vue')
  },
  {
    path: '/screener',
    name: 'Screener',
    component: () => import('../views/ScreenerView.vue')
  },
  {
    path: '/news',
    name: 'News',
    component: () => import('../views/NewsView.vue')
  },
  {
    path: '/ai-agents',
    name: 'AIAgents',
    component: () => import('../views/AIAgentsView.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router

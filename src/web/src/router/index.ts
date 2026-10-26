import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router'
import { session } from '../session'

export const routes = [
  { path: '/', name: 'login', component: () => import('../pages/LoginPage.vue') },
  { path: '/home', name: 'home', component: () => import('../pages/HomePage.vue') },
  { path: '/deposit', name: 'deposit', component: () => import('../pages/DepositPage.vue') },
  { path: '/withdraw', name: 'withdraw', component: () => import('../pages/WithdrawPage.vue') },
  { path: '/transfer', name: 'transfer', component: () => import('../pages/TransferPage.vue') },
  { path: '/processing', name: 'processing', component: () => import('../pages/ProcessingPage.vue') },
  { path: '/result', name: 'result', component: () => import('../pages/ResultPage.vue') },
  { path: '/history', name: 'history', component: () => import('../pages/HistoryPage.vue') },
]

export function requireSession(to: RouteLocationNormalized) {
  if (!session.userId && to.name !== 'login') return '/'
}

const router = createRouter({ history: createWebHistory(), routes })
router.beforeEach(requireSession)
export default router

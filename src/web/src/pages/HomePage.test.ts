import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import HomePage from './HomePage.vue'
import { session, clearSession } from '../session'

async function setup() {
  const stub = { template: '<i/>' }
  const router = createRouter({ history: createMemoryHistory(), routes: [
    { path: '/', component: stub }, { path: '/home', component: HomePage },
    ...['deposit', 'withdraw', 'transfer', 'history'].map(p => ({ path: '/' + p, component: stub }))] })
  router.push('/home'); await router.isReady()
  return { router, w: mount(HomePage, { global: { plugins: [router] } }) }
}

describe('HomePage', () => {
  beforeEach(() => { clearSession(); session.userId = 'u1'; session.userName = 'user-1' })

  it('exit_clears_session_and_returns_to_login', async () => {
    const { w, router } = await setup()
    await w.get('[data-testid=exit]').trigger('click')
    await new Promise(r => setTimeout(r))
    expect(session.userId).toBeNull()
    expect(router.currentRoute.value.path).toBe('/')
  })

  it('home_buttons_navigate_to_their_pages', async () => {
    for (const [id, path] of [['deposit', '/deposit'], ['withdraw', '/withdraw'], ['transfer', '/transfer'], ['history', '/history']]) {
      const { w, router } = await setup()
      await w.get(`[data-testid=${id}]`).trigger('click')
      await new Promise(r => setTimeout(r))
      expect(router.currentRoute.value.path).toBe(path)
    }
  })
})

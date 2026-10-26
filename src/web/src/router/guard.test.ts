import { describe, it, expect, beforeEach } from 'vitest'
import { createRouter, createMemoryHistory } from 'vue-router'
import { routes, requireSession } from './index'
import { session, clearSession } from '../session'

function makeRouter() {
  const stub = { template: '<i/>' }
  const r = createRouter({ history: createMemoryHistory(), routes: routes.map((x) => ({ ...x, component: stub })) })
  r.beforeEach(requireSession)
  return r
}

describe('router guard', () => {
  beforeEach(() => clearSession())

  it.each(['/deposit', '/history', '/home', '/withdraw', '/transfer'])('redirects_%s_to_login_without_session', async (path) => {
    const r = makeRouter()
    await r.push(path)
    expect(r.currentRoute.value.path).toBe('/')
  })

  it('login_is_reachable_without_session', async () => {
    const r = makeRouter()
    await r.push('/')
    expect(r.currentRoute.value.path).toBe('/')
  })

  it('allows_protected_routes_with_session', async () => {
    session.userId = 'u1'
    const r = makeRouter()
    await r.push('/history')
    expect(r.currentRoute.value.path).toBe('/history')
  })
})

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import LoginPage from './LoginPage.vue'
import { session, clearSession } from '../session'
import { ApiError } from '../api/client'

const api = vi.hoisted(() => ({ getUserByName: vi.fn(), resetDemo: vi.fn() }))
vi.mock('../api/client', async (orig) => ({ ...(await orig<typeof import('../api/client')>()), ...api }))

async function setup() {
  const router = createRouter({ history: createMemoryHistory(), routes: [
    { path: '/', component: LoginPage }, { path: '/home', component: { template: '<i/>' } }] })
  router.push('/'); await router.isReady()
  return { router, w: mount(LoginPage, { global: { plugins: [router] } }) }
}

describe('LoginPage', () => {
  beforeEach(() => { vi.clearAllMocks(); clearSession() })

  it('enter_loads_user_1_and_navigates_home', async () => {
    api.getUserByName.mockResolvedValue({ id: 'u1', name: 'user-1' })
    const { w, router } = await setup()
    await w.get('[data-testid=enter]').trigger('click'); await flushPromises()
    expect(api.getUserByName).toHaveBeenCalledWith('user-1')
    expect(session.userId).toBe('u1'); expect(session.userName).toBe('user-1')
    expect(router.currentRoute.value.path).toBe('/home')
  })

  it('enter_shows_service_unavailable_when_api_is_down', async () => {
    api.getUserByName.mockRejectedValue(new ApiError('Service unavailable', 0, {}, true))
    const { w, router } = await setup()
    await w.get('[data-testid=enter]').trigger('click'); await flushPromises()
    expect(w.get('[role=alert]').text()).toBe('Service unavailable')
    expect(router.currentRoute.value.path).toBe('/')
  })

  it('reset_requires_confirmation_before_calling_the_api', async () => {
    api.getUserByName.mockResolvedValue({ id: 'u1', name: 'user-1' })
    api.resetDemo.mockResolvedValue(undefined)
    const { w } = await setup()
    await w.get('[data-testid=reset]').trigger('click'); await flushPromises()
    expect(api.resetDemo).not.toHaveBeenCalled()
    await w.get('[data-testid=reset-confirm]').trigger('click'); await flushPromises()
    expect(api.resetDemo).toHaveBeenCalledWith('u1')
  })
})

describe('LoginPage errors', () => {
  it('enter_shows_generic_message_for_non_api_errors', async () => {
    api.getUserByName.mockRejectedValue(new Error('boom'))
    const { w } = await setup()
    await w.get('[data-testid=enter]').trigger('click'); await flushPromises()
    expect(w.get('[role=alert]').text()).toBe('An unexpected error occurred')
  })
})

describe('LoginPage api error message', () => {
  it('enter_shows_api_error_message_for_non_unavailable_api_errors', async () => {
    api.getUserByName.mockRejectedValue(new ApiError('User not found', 404))
    const { w } = await setup()
    await w.get('[data-testid=enter]').trigger('click'); await flushPromises()
    expect(w.get('[role=alert]').text()).toBe('User not found')
  })
})

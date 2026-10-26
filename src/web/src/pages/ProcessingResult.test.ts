import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import ProcessingPage from './ProcessingPage.vue'
import ResultPage from './ResultPage.vue'
import { session, clearSession } from '../session'
import { ApiError } from '../api/client'

const stub = { template: '<i/>' }
async function mountAt(path: string) {
  const router = createRouter({ history: createMemoryHistory(), routes: [
    { path: '/', component: stub }, { path: '/deposit', component: stub },
    { path: '/processing', component: ProcessingPage }, { path: '/result', component: ResultPage }] })
  router.push(path)
  await router.isReady()
  const w = mount({ template: '<router-view/>' }, { global: { plugins: [router] } })
  return { router, w }
}

describe('processing and result', () => {
  beforeEach(() => { clearSession(); vi.useFakeTimers() })
  afterEach(() => vi.useRealTimers())

  it('processing_without_pending_operation_redirects_to_login', async () => {
    const { router } = await mountAt('/processing')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/')
  })

  it('result_without_outcome_redirects_to_login', async () => {
    const { router } = await mountAt('/result')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/')
  })

  it('result_shows_new_balances_on_success_and_message_on_failure', async () => {
    session.status = 'done'
    session.outcome = { ok: true, message: 'Deposited $100.00', result: { transactionId: 't', type: 'Deposit',
      balances: [{ id: 'c', type: 'Checking', balance: 1100 }] } }
    const { w } = await mountAt('/result')
    expect(w.get('[data-testid=success]').text()).toContain('Deposited $100.00')
    expect(w.text()).toContain('Checking: $1100.00')
    session.outcome = { ok: false, error: 'Insufficient funds' }
    await flushPromises()
    expect(w.get('[data-testid=failure]').text()).toBe('Insufficient funds')
  })

  it('result_shows_service_unavailable_when_api_is_down', async () => {
    const call = vi.fn(() => Promise.reject(new ApiError('Service unavailable', 0, {}, true)))
    session.staged = { call, message: 'x' }
    const { router, w } = await mountAt('/processing')
    await vi.advanceTimersByTimeAsync(1500)
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/result')
    expect(w.get('[data-testid=failure]').text()).toBe('Service unavailable')
  })

  it('back_from_result_does_not_resubmit', async () => {
    const call = vi.fn(() => Promise.resolve({ transactionId: 't', type: 'Deposit', balances: [] }))
    session.staged = { call, message: 'ok' }
    const { router } = await mountAt('/deposit')
    await router.push('/processing')
    await vi.advanceTimersByTimeAsync(1500)
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/result')
    router.back()
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/deposit')
    expect(call).toHaveBeenCalledTimes(1)
    await router.push('/processing')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/')
    expect(call).toHaveBeenCalledTimes(1)
  })

  it('done_clears_session_and_returns_to_login', async () => {
    session.userId = 'u1'
    session.status = 'done'
    session.outcome = { ok: false, error: 'x' }
    const { router, w } = await mountAt('/result')
    await w.get('[data-testid=done]').trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/')
    expect(session.userId).toBeNull()
    expect(session.outcome).toBeNull()
  })
})

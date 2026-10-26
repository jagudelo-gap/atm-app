import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import HistoryPage from './HistoryPage.vue'
import { session, clearSession } from '../session'
import * as api from '../api/client'

vi.mock('../api/client', async (orig) => ({ ...(await orig<typeof api>()), getAccounts: vi.fn(), getHistory: vi.fn() }))

const accounts = [{ id: 'a1', type: 'Checking', balance: 100 }, { id: 'a2', type: 'Savings', balance: 250.5 }]
const tx = (o: Partial<api.TransactionDto>): api.TransactionDto => ({
  id: 'x', type: 'Deposit', status: 'Completed', amount: 10, accountId: 'a1', accountType: 'Checking',
  createdAt: '2026-10-23T10:00:00Z', ...o })

async function setup(history: api.TransactionDto[]) {
  vi.mocked(api.getAccounts).mockResolvedValue(accounts)
  vi.mocked(api.getHistory).mockResolvedValue(history)
  const stub = { template: '<i/>' }
  const router = createRouter({ history: createMemoryHistory(), routes: [
    { path: '/home', component: stub }, { path: '/history', component: HistoryPage }] })
  router.push('/history'); await router.isReady()
  const w = mount(HistoryPage, { global: { plugins: [router] } })
  await flushPromises()
  return { w, router }
}

describe('HistoryPage', () => {
  beforeEach(() => { clearSession(); session.userId = 'u1' })

  it('shows_balances_and_rows_in_api_order', async () => {
    const { w } = await setup([tx({ id: '1', type: 'Withdraw', amount: 5 }), tx({ id: '2', type: 'Deposit', amount: 7 })])
    expect(w.text()).toContain('Checking')
    expect(w.text()).toContain('100.00')
    expect(w.text()).toContain('250.50')
    const rows = w.findAll('[data-testid=row]')
    expect(rows).toHaveLength(2)
    expect(rows[0].text()).toContain('Withdraw')
    expect(rows[1].text()).toContain('Deposit')
  })

  it('transfer_row_shows_from_and_to', async () => {
    const { w } = await setup([tx({ type: 'Transfer', toAccountId: 'a2', toAccountType: 'Savings' })])
    expect(w.get('[data-testid=row]').text()).toContain('Checking → Savings')
  })

  it('failed_row_shows_reason', async () => {
    const { w } = await setup([tx({ status: 'Failed', failureReason: 'Insufficient funds' })])
    const reason = w.get('[data-testid=reason]')
    expect(reason.text()).toContain('Insufficient funds')
    expect(reason.classes()).toContain('text-error')
  })

  it('empty_state_is_shown', async () => {
    const { w } = await setup([])
    expect(w.text()).toContain('No transactions yet')
    expect(w.find('[data-testid=row]').exists()).toBe(false)
  })

  it('back_returns_home', async () => {
    const { w, router } = await setup([])
    await w.get('[data-testid=back]').trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/home')
  })
})

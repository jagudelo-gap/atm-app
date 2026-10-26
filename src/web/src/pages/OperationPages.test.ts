import { describe, it, expect, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import DepositPage from './DepositPage.vue'
import WithdrawPage from './WithdrawPage.vue'
import TransferPage from './TransferPage.vue'
import { session, clearSession } from '../session'

const stub = { template: '<i/>' }
async function mountPage(page: object) {
  const router = createRouter({ history: createMemoryHistory(), routes: [
    { path: '/', component: stub }, { path: '/home', component: stub },
    { path: '/p', component: page }, { path: '/processing', component: stub }] })
  router.push('/p')
  await router.isReady()
  return { router, w: mount(page, { global: { plugins: [router] } }) }
}
async function press(keys: string) {
  for (const k of keys) window.dispatchEvent(new KeyboardEvent('keydown', { key: k }))
  await flushPromises()
}

describe('operation pages', () => {
  beforeEach(() => {
    clearSession()
    session.userId = 'u1'
    session.accounts = [{ id: 'c', type: 'Checking', balance: 1 }, { id: 's', type: 'Savings', balance: 2 }]
  })

  it('submit_is_disabled_until_account_and_amount_are_set', async () => {
    const { w } = await mountPage(DepositPage)
    const submit = () => w.get('[data-testid=submit]').attributes('disabled')
    expect(submit()).toBeDefined()
    await press('5')
    expect(submit()).toBeDefined()
    await w.get('[data-testid=account-checking]').trigger('click')
    expect(submit()).toBeUndefined()
    w.unmount()
  })

  it('transfer_to_account_is_the_other_account', async () => {
    const { w } = await mountPage(TransferPage)
    await w.get('[data-testid=account-checking]').trigger('click')
    expect(w.get('[data-testid=to-account]').text()).toBe('To: Savings')
    await w.get('[data-testid=account-savings]').trigger('click')
    expect(w.get('[data-testid=to-account]').text()).toBe('To: Checking')
    w.unmount()
  })

  it('withdraw_page_uses_whole_dollar_entry', async () => {
    const { w } = await mountPage(WithdrawPage)
    await press('25')
    expect(w.get('[data-testid=amount-display]').text()).toBe('$25')
    w.unmount()
  })

  it('submit_stages_once_and_goes_to_processing_even_when_double_clicked', async () => {
    const { w, router } = await mountPage(DepositPage)
    await w.get('[data-testid=account-checking]').trigger('click')
    await press('100')
    await w.get('[data-testid=submit]').trigger('click')
    await w.get('[data-testid=submit]').trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/processing')
    expect(session.staged?.message).toBe('Deposited $1.00')
    w.unmount()
  })
})

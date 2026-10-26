import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { useOperation } from './useOperation'
import { session, clearSession } from '../session'
import { ApiError } from '../api/client'

const ok = { transactionId: 't', type: 'Deposit', balances: [] }

describe('useOperation', () => {
  beforeEach(() => { clearSession(); vi.useFakeTimers() })
  afterEach(() => vi.useRealTimers())

  it('run_waits_at_least_1500ms_even_when_api_is_instant', async () => {
    const p = useOperation().run(() => Promise.resolve(ok), 'Deposited $1.00')
    await vi.advanceTimersByTimeAsync(1499)
    expect(session.status).toBe('pending')
    await vi.advanceTimersByTimeAsync(1)
    await p
    expect(session.status).toBe('done')
    expect(session.outcome).toEqual({ ok: true, message: 'Deposited $1.00', result: ok })
  })

  it('double_run_calls_api_once', async () => {
    const call = vi.fn(() => Promise.resolve(ok))
    const { run } = useOperation()
    const a = run(call)
    const b = run(call)
    await vi.advanceTimersByTimeAsync(1500)
    await Promise.all([a, b])
    expect(call).toHaveBeenCalledTimes(1)
  })

  it('failure_also_waits_and_stores_api_message_or_fallback', async () => {
    const p = useOperation().run(() => Promise.reject(new ApiError('Insufficient funds', 400)))
    await vi.advanceTimersByTimeAsync(1499)
    expect(session.status).toBe('pending')
    await vi.advanceTimersByTimeAsync(1)
    await p
    expect(session.outcome).toEqual({ ok: false, error: 'Insufficient funds' })
    clearSession()
    const q = useOperation().run(() => Promise.reject(new Error('boom')))
    await vi.advanceTimersByTimeAsync(1500)
    await q
    expect(session.outcome).toEqual({ ok: false, error: 'An unexpected error occurred' })
  })
})

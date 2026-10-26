import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, deposit, getAccounts } from './client'

const json = (status: number, body: unknown) =>
  Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }))

afterEach(() => vi.unstubAllGlobals())

describe('api client', () => {
  it('maps_problem_details_to_ApiError_with_field_errors', async () => {
    vi.stubGlobal('fetch', vi.fn(() => json(400, { status: 400, title: 'Validation failed', errors: { amount: ['Amount must be positive'] } })))
    const err = await deposit('user-1', 'a1', -5).catch((e) => e)
    expect(err).toBeInstanceOf(ApiError)
    expect(err.status).toBe(400)
    expect(err.fieldErrors.amount).toEqual(['Amount must be positive'])
    expect(err.unavailable).toBe(false)
  })

  it('network_failure_sets_unavailable_and_message_Service_unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('Failed to fetch'))))
    const err = await getAccounts('user-1').catch((e) => e)
    expect(err).toBeInstanceOf(ApiError)
    expect(err.unavailable).toBe(true)
    expect(err.message).toBe('Service unavailable')
  })

  it('server_error_503_sets_unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn(() => json(503, {})))
    const err = await getAccounts('user-1').catch((e) => e)
    expect(err.unavailable).toBe(true)
    expect(err.status).toBe(503)
    expect(err.message).toBe('Service unavailable')
  })

  it('server_error_500_shows_problem_detail_and_is_not_unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn(() => json(500, { status: 500, detail: 'An unexpected error occurred' })))
    const err = await getAccounts('user-1').catch((e) => e)
    expect(err.unavailable).toBe(false)
    expect(err.status).toBe(500)
    expect(err.message).toBe('An unexpected error occurred')
  })

  it('server_error_500_without_body_falls_back_to_generic_message', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response('', { status: 500 }))))
    const err = await getAccounts('user-1').catch((e) => e)
    expect(err.unavailable).toBe(false)
    expect(err.message).toBe('An unexpected error occurred')
  })

  it('request_passes_timeout_signal_and_abort_maps_to_unavailable', async () => {
    let signal: AbortSignal | null | undefined
    const fetchMock = vi.fn((_u: string, init: RequestInit) => {
      signal = init.signal
      return Promise.reject(new DOMException('timed out', 'TimeoutError'))
    })
    vi.stubGlobal('fetch', fetchMock)
    const err = await getAccounts('user-1').catch((e) => e)
    expect(signal).toBeInstanceOf(AbortSignal)
    expect(err).toBeInstanceOf(ApiError)
    expect(err.unavailable).toBe(true)
    expect(err.message).toBe('Service unavailable')
  })

  it('deposit_posts_to_the_deposit_route_with_amount', async () => {
    const fetchMock = vi.fn(() => json(200, { transactionId: 't1', type: 'Deposit', balances: [] }))
    vi.stubGlobal('fetch', fetchMock)
    await deposit('user-1', 'a1', 25)
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toBe('/api/users/user-1/transactions/deposit')
    expect(init.method).toBe('POST')
    expect(JSON.parse(init.body as string)).toEqual({ accountId: 'a1', amount: 25 })
  })
})

import { ApiError, type OperationResult } from '../api/client'
import { session } from '../session'

export const MIN_PROCESSING_MS = 1500
const delay = (ms: number) => new Promise<void>(r => setTimeout(r, ms))
const money = (n: number) => `$${n.toFixed(2)}`

export const messages = {
  deposit: (n: number) => `Deposited ${money(n)}`,
  withdraw: (n: number) => `Withdrew ${money(n)}`,
  transfer: (n: number) => `Transferred ${money(n)}`,
}

export function stage(call: () => Promise<OperationResult>, message: string) {
  if (session.status === 'pending') return
  session.outcome = null
  session.status = 'idle'
  session.staged = { call, message }
}

export function useOperation() {
  async function run(call: () => Promise<OperationResult>, message = '') {
    if (session.status === 'pending') return
    session.status = 'pending'
    session.outcome = null
    const attempt = call().then(
      result => ({ ok: true as const, result }),
      (e: unknown) => ({ ok: false as const, e }),
    )
    const [settled] = await Promise.all([attempt, delay(MIN_PROCESSING_MS)])
    if (settled.ok) {
      session.lastResult = settled.result
      session.outcome = { ok: true, message, result: settled.result }
    } else {
      const e = settled.e
      session.outcome = { ok: false, error: e instanceof ApiError && e.message ? e.message : 'An unexpected error occurred' }
    }
    session.status = 'done'
  }
  return { run }
}

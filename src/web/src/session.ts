import { reactive } from 'vue'
import type { AccountDto, OperationResult } from './api/client'

export type Outcome =
  | { ok: true; message: string; result: OperationResult }
  | { ok: false; error: string }

export interface StagedOperation { call: () => Promise<OperationResult>; message: string }

export const session = reactive({
  userId: null as string | null,
  userName: null as string | null,
  accounts: [] as AccountDto[],
  lastResult: null as OperationResult | null,
  staged: null as StagedOperation | null,
  status: 'idle' as 'idle' | 'pending' | 'done',
  outcome: null as Outcome | null,
})

export function clearSession() {
  session.userId = null
  session.userName = null
  session.accounts = []
  session.lastResult = null
  session.staged = null
  session.status = 'idle'
  session.outcome = null
}

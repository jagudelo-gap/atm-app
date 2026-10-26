export interface AccountDto { id: string; type: string; balance: number }
export interface UserDto { id: string; name: string }
export interface OperationResult { transactionId: string; type: string; balances: AccountDto[] }
export interface TransactionDto {
  id: string; type: string; status: string; amount: number
  accountId: string; accountType: string
  toAccountId?: string; toAccountType?: string
  failureReason?: string; createdAt: string
}

export class ApiError extends Error {
  status: number
  fieldErrors: Record<string, string[]>
  unavailable: boolean
  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}, unavailable = false) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
    this.unavailable = unavailable
  }
}

async function request<T>(url: string, method = 'GET', body?: unknown): Promise<T> {
  let res: Response
  try {
    res = await fetch(url, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(10000),
    })
  } catch {
    throw new ApiError('Service unavailable', 0, {}, true)
  }
  if (res.ok) return (res.status === 204 ? undefined : await res.json()) as T
  if ([502, 503, 504].includes(res.status)) throw new ApiError('Service unavailable', res.status, {}, true)
  const problem = await res.json().catch(() => ({}))
  const fallback = res.status >= 500 ? 'An unexpected error occurred' : 'Request failed'
  throw new ApiError(problem.detail || (res.status >= 500 ? '' : problem.title) || fallback, res.status, problem.errors ?? {})
}

const tx = (userId: string, op: string) => `/api/users/${encodeURIComponent(userId)}/transactions/${op}`

export const getUserByName = (name: string) => request<UserDto>(`/api/users/by-name/${encodeURIComponent(name)}`)
export const getAccounts = (userId: string) => request<AccountDto[]>(`/api/users/${encodeURIComponent(userId)}/accounts`)
export const deposit = (userId: string, accountId: string, amount: number) =>
  request<OperationResult>(tx(userId, 'deposit'), 'POST', { accountId, amount })
export const withdraw = (userId: string, accountId: string, amount: number) =>
  request<OperationResult>(tx(userId, 'withdraw'), 'POST', { accountId, amount })
export const transfer = (userId: string, fromAccountId: string, toAccountId: string, amount: number) =>
  request<OperationResult>(tx(userId, 'transfer'), 'POST', { fromAccountId, toAccountId, amount })
export const getHistory = (userId: string) =>
  request<TransactionDto[]>(`/api/users/${encodeURIComponent(userId)}/transactions`)
export const resetDemo = (userId: string) => request<void>('/api/demo/reset', 'POST', { userId })

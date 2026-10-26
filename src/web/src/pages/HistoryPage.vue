<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getAccounts, getHistory, type AccountDto, type TransactionDto } from '../api/client'
import { session } from '../session'

const router = useRouter()
const accounts = ref<AccountDto[]>([])
const rows = ref<TransactionDto[]>([])
const error = ref('')
const loaded = ref(false)

const money = (n: number) => n.toLocaleString('en-US', { style: 'currency', currency: 'USD' })
const account = (t: TransactionDto) =>
  t.toAccountType ? `${t.accountType} → ${t.toAccountType}` : t.accountType

onMounted(async () => {
  try {
    const id = session.userId ?? ''
    ;[accounts.value, rows.value] = await Promise.all([getAccounts(id), getHistory(id)])
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Request failed'
  }
  loaded.value = true
})
</script>

<template>
  <div>
    <h1 class="text-center">Balances &amp; History</h1>
    <p v-if="error" class="text-error text-center" role="alert">{{ error }}</p>
    <div class="row text-center my-3">
      <div v-for="a in accounts" :key="a.id" class="col">
        <div class="text-muted">{{ a.type }}</div>
        <div class="fs-3" data-testid="balance">{{ money(a.balance) }}</div>
      </div>
    </div>
    <p v-if="loaded && !error && !rows.length" class="text-center">No transactions yet</p>
    <table v-else-if="rows.length" class="table">
      <thead><tr><th>Date</th><th>Type</th><th>Account</th><th>Amount</th><th>Status</th></tr></thead>
      <tbody>
        <tr v-for="t in rows" :key="t.id" data-testid="row">
          <td>{{ new Date(t.createdAt).toLocaleString() }}</td>
          <td>{{ t.type }}</td>
          <td>{{ account(t) }}</td>
          <td>{{ money(t.amount) }}</td>
          <td>
            {{ t.status }}
            <span v-if="t.status === 'Failed' && t.failureReason" class="text-error" data-testid="reason">
              {{ t.failureReason }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
    <div class="text-center">
      <button class="btn btn-outline-secondary" data-testid="back" @click="router.push('/home')">Back</button>
    </div>
  </div>
</template>

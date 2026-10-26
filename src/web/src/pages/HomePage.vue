<script setup lang="ts">
import { useRouter } from 'vue-router'
import { clearSession } from '../session'

const router = useRouter()
const items = [
  { id: 'deposit', label: 'Deposit', path: '/deposit' },
  { id: 'withdraw', label: 'Withdraw', path: '/withdraw' },
  { id: 'transfer', label: 'Transfer', path: '/transfer' },
  { id: 'history', label: 'Balances & History', path: '/history' },
]

function exit() {
  clearSession()
  router.push('/')
}
</script>

<template>
  <div class="text-center">
    <h1>Choose an operation</h1>
    <div class="d-grid gap-3 col-md-6 mx-auto my-4">
      <button v-for="i in items" :key="i.id" class="btn btn-primary btn-lg" :data-testid="i.id" @click="router.push(i.path)">
        {{ i.label }}
      </button>
    </div>
    <button class="btn btn-outline-secondary" data-testid="exit" @click="exit">Exit</button>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { session, clearSession } from '../session'

const router = useRouter()
onMounted(() => { if (session.status !== 'done' || !session.outcome) router.replace('/') })

function done() {
  clearSession()
  router.push('/')
}
</script>

<template>
  <div v-if="session.outcome" class="text-center py-4">
    <div v-if="session.outcome.ok" data-testid="success" class="text-success">
      <h1>{{ session.outcome.message }}</h1>
      <p v-for="b in session.outcome.result.balances" :key="b.id" class="text-body">{{ b.type }}: ${{ b.balance.toFixed(2) }}</p>
    </div>
    <div v-else data-testid="failure" role="alert" class="text-error border border-danger rounded p-3 d-inline-block">
      <h1>{{ session.outcome.error }}</h1>
    </div>
    <div class="mt-4"><button class="btn btn-primary btn-lg" data-testid="done" @click="done">Done</button></div>
  </div>
</template>

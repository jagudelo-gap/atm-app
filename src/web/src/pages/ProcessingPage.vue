<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { session } from '../session'
import { useOperation } from '../composables/useOperation'

const router = useRouter()
const { run } = useOperation()

onMounted(async () => {
  const op = session.staged
  if (!op || session.status !== 'idle') { router.replace('/'); return }
  session.staged = null
  await run(op.call, op.message)
  router.replace('/result')
})
</script>

<template>
  <div class="text-center py-5">
    <div class="spinner-border text-primary" role="status" aria-hidden="true"></div>
    <h1 class="mt-3">Processing...</h1>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { getUserByName, resetDemo, ApiError } from '../api/client'
import { session } from '../session'

const router = useRouter()
const error = ref('')
const confirming = ref(false)
const notice = ref('')

function fail(e: unknown) {
  error.value = e instanceof ApiError ? e.message : 'An unexpected error occurred'
}

async function enter() {
  error.value = ''
  notice.value = ''
  try {
    const user = await getUserByName('user-1')
    session.userId = user.id
    session.userName = user.name
    await router.push('/home')
  } catch (e) { fail(e) }
}

async function reset() {
  error.value = ''
  notice.value = ''
  confirming.value = false
  try {
    const user = await getUserByName('user-1')
    await resetDemo(user.id)
    notice.value = 'Demo data reset'
  } catch (e) { fail(e) }
}
</script>

<template>
  <div class="text-center">
    <h1>Welcome</h1>
    <button class="btn btn-primary btn-lg my-3" data-testid="enter" @click="enter">Enter</button>
    <p v-if="error" role="alert" class="text-error border border-danger rounded p-2 d-inline-block">{{ error }}</p>
    <p v-if="notice" role="status">{{ notice }}</p>
    <div class="mt-5">
      <button v-if="!confirming" class="btn btn-outline-secondary btn-sm" data-testid="reset" @click="confirming = true">
        Reset demo data (demo only)
      </button>
      <div v-else>
        <span>Reset all demo data?</span>
        <button class="btn btn-danger btn-sm ms-2" data-testid="reset-confirm" @click="reset">Confirm</button>
        <button class="btn btn-link btn-sm" data-testid="reset-cancel" @click="confirming = false">Cancel</button>
      </div>
    </div>
  </div>
</template>

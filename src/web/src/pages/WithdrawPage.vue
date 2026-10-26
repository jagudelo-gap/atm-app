<script setup lang="ts">
import { useRouter } from 'vue-router'
import { withdraw } from '../api/client'
import { session } from '../session'
import { stage, messages } from '../composables/useOperation'
import OperationForm from '../components/OperationForm.vue'

const router = useRouter()
function onSubmit(p: { from: string; amount: number }) {
  stage(() => withdraw(session.userId!, p.from, p.amount), messages.withdraw(p.amount))
  router.push('/processing')
}
</script>

<template><OperationForm title="Withdraw" whole-dollars @submit="onSubmit" /></template>

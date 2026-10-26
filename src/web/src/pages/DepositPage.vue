<script setup lang="ts">
import { useRouter } from 'vue-router'
import { deposit } from '../api/client'
import { session } from '../session'
import { stage, messages } from '../composables/useOperation'
import OperationForm from '../components/OperationForm.vue'

const router = useRouter()
function onSubmit(p: { from: string; amount: number }) {
  stage(() => deposit(session.userId!, p.from, p.amount), messages.deposit(p.amount))
  router.push('/processing')
}
</script>

<template><OperationForm title="Deposit" :whole-dollars="false" @submit="onSubmit" /></template>

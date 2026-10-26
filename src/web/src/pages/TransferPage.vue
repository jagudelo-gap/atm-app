<script setup lang="ts">
import { useRouter } from 'vue-router'
import { transfer } from '../api/client'
import { session } from '../session'
import { stage, messages } from '../composables/useOperation'
import OperationForm from '../components/OperationForm.vue'

const router = useRouter()
function onSubmit(p: { from: string; to: string; amount: number }) {
  stage(() => transfer(session.userId!, p.from, p.to, p.amount), messages.transfer(p.amount))
  router.push('/processing')
}
</script>

<template><OperationForm title="Transfer" :whole-dollars="false" transfer @submit="onSubmit" /></template>

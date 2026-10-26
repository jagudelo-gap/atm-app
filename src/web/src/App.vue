<script setup lang="ts">
import { session } from './session'
</script>

<template>
  <header class="atm-header d-flex justify-content-between align-items-center px-4 py-3">
    <span class="atm-heading fs-4">ATM</span>
    <span v-if="session.userId" data-testid="user">{{ session.userName ?? session.userId }}</span>
  </header>
  <main class="container py-4"><router-view /></main>
</template>

<script setup lang="ts">
defineProps<{ value: string; label?: string }>()
</script>

<template>
  <div class="text-center my-3">
    <div v-if="label" class="text-muted small">{{ label }}</div>
    <div class="amount-display" data-testid="amount-display">{{ value }}</div>
  </div>
</template>

<style scoped>
.amount-display {
  font-family: 'Fira Sans Condensed', sans-serif;
  font-size: 3rem;
  font-weight: 600;
  color: var(--atm-navy);
  border-bottom: 3px solid var(--atm-primary);
}
</style>

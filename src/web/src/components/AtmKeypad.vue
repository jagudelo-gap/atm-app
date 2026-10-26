<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'

const emit = defineEmits<{
  digit: [d: string]
  clear: []
  backspace: []
  enter: []
  cancel: []
}>()

const rows = [['1', '2', '3'], ['4', '5', '6'], ['7', '8', '9']]

function onKey(e: KeyboardEvent) {
  const t = e.target as HTMLElement | null
  if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.isContentEditable)) return
  let handled = true
  if (/^\d$/.test(e.key)) emit('digit', e.key)
  else if (e.key === 'Backspace') emit('backspace')
  else if (e.key === 'Delete') emit('clear')
  else if (e.key === 'Enter') emit('enter')
  else if (e.key === 'Escape') emit('cancel')
  else handled = false
  if (handled) e.preventDefault()
}

onMounted(() => window.addEventListener('keydown', onKey))
onUnmounted(() => window.removeEventListener('keydown', onKey))
</script>

<template>
  <div class="container-fluid atm-keypad">
    <div v-for="(row, i) in rows" :key="i" class="row g-2 mb-2">
      <div v-for="d in row" :key="d" class="col-4">
        <button type="button" class="btn btn-outline-primary w-100 py-3 fs-4" @click="emit('digit', d)">{{ d }}</button>
      </div>
    </div>
    <div class="row g-2 mb-2">
      <div class="col-4"><button type="button" class="btn btn-warning w-100 py-3" @click="emit('clear')">Clear</button></div>
      <div class="col-4"><button type="button" class="btn btn-outline-primary w-100 py-3 fs-4" @click="emit('digit', '0')">0</button></div>
      <div class="col-4"><button type="button" class="btn btn-secondary w-100 py-3" @click="emit('backspace')">Backspace</button></div>
    </div>
    <div class="row g-2">
      <div class="col-6"><button type="button" class="btn btn-danger w-100 py-3" @click="emit('cancel')">Cancel</button></div>
      <div class="col-6"><button type="button" class="btn btn-primary w-100 py-3" @click="emit('enter')">Enter</button></div>
    </div>
  </div>
</template>

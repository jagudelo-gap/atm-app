<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getAccounts, ApiError } from '../api/client'
import { session } from '../session'
import { useAmountEntry } from '../composables/useAmountEntry'
import AtmKeypad from './AtmKeypad.vue'
import AmountDisplay from './AmountDisplay.vue'

const props = defineProps<{ title: string; wholeDollars: boolean; transfer?: boolean }>()
const emit = defineEmits<{ submit: [payload: { from: string; to: string; amount: number }] }>()

const router = useRouter()
const entry = useAmountEntry({ wholeDollars: props.wholeDollars })
const chosen = ref('')
const error = ref('')
const submitted = ref(false)

const other = computed(() => session.accounts.find(a => a.id !== chosen.value))
const to = computed(() => (props.transfer ? other.value?.id ?? '' : chosen.value))
const canSubmit = computed(() =>
  !submitted.value && !!chosen.value && entry.amount.value > 0 && (!props.transfer || (!!to.value && to.value !== chosen.value)))

function submit() {
  if (!canSubmit.value) return
  submitted.value = true
  emit('submit', { from: chosen.value, to: to.value, amount: entry.amount.value })
}
const cancel = () => router.push('/home')

onMounted(async () => {
  if (session.accounts.length || !session.userId) return
  try { session.accounts = await getAccounts(session.userId) }
  catch (e) { error.value = e instanceof ApiError && e.unavailable ? 'Service unavailable' : 'An unexpected error occurred' }
})
</script>

<template>
  <div class="container py-3">
    <h1 class="text-center">{{ title }}</h1>
    <p v-if="error" role="alert" class="text-error border border-danger rounded p-2">{{ error }}</p>
    <div class="d-flex gap-2 justify-content-center">
      <button v-for="a in session.accounts" :key="a.id" type="button" :data-testid="`account-${a.type.toLowerCase()}`"
        :class="['btn', chosen === a.id ? 'btn-primary' : 'btn-outline-primary']" @click="chosen = a.id">
        {{ transfer ? 'From ' : '' }}{{ a.type }}
      </button>
    </div>
    <p v-if="transfer && other && chosen" class="text-center mt-2" data-testid="to-account">To: {{ other.type }}</p>
    <AmountDisplay :value="entry.display.value" />
    <AtmKeypad @digit="entry.press" @clear="entry.clear" @backspace="entry.backspace" @enter="submit" @cancel="cancel" />
    <div class="d-flex gap-2 mt-3">
      <button type="button" class="btn btn-danger flex-fill" data-testid="cancel" @click="cancel">Cancel</button>
      <button type="button" class="btn btn-primary flex-fill" data-testid="submit" :disabled="!canSubmit" @click="submit">Submit</button>
    </div>
  </div>
</template>

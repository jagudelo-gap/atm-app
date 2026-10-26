import { computed, ref } from 'vue'

export function useAmountEntry(options: { wholeDollars: boolean; maxDigits?: number }) {
  const max = options.maxDigits ?? (options.wholeDollars ? 7 : 9)
  const digits = ref('')

  const amount = computed(() => {
    const n = Number(digits.value || '0')
    return options.wholeDollars ? n : n / 100
  })
  const display = computed(() =>
    options.wholeDollars ? `$${amount.value}` : `$${amount.value.toFixed(2)}`,
  )

  function press(d: string) {
    if (!/^\d$/.test(d)) return
    if (digits.value === '' && d === '0') return
    if (digits.value.length >= max) return
    digits.value += d
  }
  const backspace = () => { digits.value = digits.value.slice(0, -1) }
  const clear = () => { digits.value = '' }

  return { digits, amount, display, press, backspace, clear }
}

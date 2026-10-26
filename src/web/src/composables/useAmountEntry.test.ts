import { describe, expect, it } from 'vitest'
import { useAmountEntry } from './useAmountEntry'

const press = (e: ReturnType<typeof useAmountEntry>, s: string) => [...s].forEach(e.press)

describe('useAmountEntry', () => {
  it('cents_mode_shifts_digits_in_from_the_right', () => {
    const e = useAmountEntry({ wholeDollars: false })
    press(e, '10000')
    expect(e.amount.value).toBe(100)
    expect(e.display.value).toBe('$100.00')
  })
  it('single_digit_shows_cents', () => {
    const e = useAmountEntry({ wholeDollars: false })
    e.press('5')
    expect(e.display.value).toBe('$0.05')
    expect(e.amount.value).toBe(0.05)
  })
  it('whole_dollar_mode_1_0_0_is_100', () => {
    const e = useAmountEntry({ wholeDollars: true })
    press(e, '100')
    expect(e.amount.value).toBe(100)
    expect(e.display.value).toBe('$100')
  })
  it('digit_cap_ignores_extra_digits', () => {
    const c = useAmountEntry({ wholeDollars: false })
    press(c, '1234567890')
    expect(c.digits.value).toBe('123456789')
    const w = useAmountEntry({ wholeDollars: true })
    press(w, '123456789')
    expect(w.digits.value).toBe('1234567')
    const z = useAmountEntry({ wholeDollars: true })
    press(z, '0012')
    expect(z.digits.value).toBe('12')
  })
  it('backspace_removes_last_digit_and_clear_empties', () => {
    const e = useAmountEntry({ wholeDollars: false })
    press(e, '123')
    e.backspace()
    expect(e.digits.value).toBe('12')
    e.clear()
    expect(e.digits.value).toBe('')
    expect(e.display.value).toBe('$0.00')
  })
})

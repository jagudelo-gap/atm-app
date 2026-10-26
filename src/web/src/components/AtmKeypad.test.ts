import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AtmKeypad from './AtmKeypad.vue'

describe('AtmKeypad', () => {
  it('keypad_emits_events_for_clicks_and_physical_keys', async () => {
    const w = mount(AtmKeypad, { attachTo: document.body })
    const click = (t: string) => w.findAll('button').find((b) => b.text() === t)!.trigger('click')
    await click('7')
    await click('Clear')
    await click('Backspace')
    await click('Enter')
    await click('Cancel')
    const key = (k: string) => window.dispatchEvent(new KeyboardEvent('keydown', { key: k }))
    key('3'); key('Backspace'); key('Delete'); key('Enter'); key('Escape')
    expect(w.emitted('digit')).toEqual([['7'], ['3']])
    expect(w.emitted('backspace')).toHaveLength(2)
    expect(w.emitted('clear')).toHaveLength(2)
    expect(w.emitted('enter')).toHaveLength(2)
    expect(w.emitted('cancel')).toHaveLength(2)
    const events = w.emitted()
    w.unmount()
    key('4')
    expect(events.digit).toHaveLength(2)
  })

  it('prevents default on handled keys and ignores input targets', () => {
    const w = mount(AtmKeypad, { attachTo: document.body })
    const ev = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true })
    window.dispatchEvent(ev)
    expect(ev.defaultPrevented).toBe(true)
    const input = document.createElement('input')
    document.body.appendChild(input)
    input.dispatchEvent(new KeyboardEvent('keydown', { key: '5', bubbles: true }))
    expect(w.emitted('digit')).toBeUndefined()
    w.unmount()
  })
})

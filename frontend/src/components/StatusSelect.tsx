import { useEffect, useState } from 'react'
import { applicationStatuses } from '../api/types'
import type { ApplicationStatus } from '../api/types'
import { label, statusTone } from '../labels'

interface Props {
  value: ApplicationStatus
  /** Saves the new status; resolves to whether it succeeded. */
  onChange: (status: ApplicationStatus) => Promise<boolean>
}

/**
 * The status badge *is* the control: a pill-styled <select> that saves on change.
 * A short "Saved" note confirms it, since there is no separate Save button.
 */
export function StatusSelect({ value, onChange }: Props) {
  const [state, setState] = useState<'idle' | 'saving' | 'saved'>('idle')

  useEffect(() => {
    if (state !== 'saved') return
    const timer = setTimeout(() => setState('idle'), 2000)
    return () => clearTimeout(timer)
  }, [state])

  const handleChange = async (status: ApplicationStatus) => {
    if (status === value) return
    setState('saving')
    setState((await onChange(status)) ? 'saved' : 'idle')
  }

  return (
    <span className="status-select">
      <select
        aria-label="Status (saves on change)"
        className={`badge badge-${statusTone(value)} badge-select`}
        value={value}
        disabled={state === 'saving'}
        onChange={(e) => void handleChange(e.target.value as ApplicationStatus)}
      >
        {applicationStatuses.map((status) => (
          <option key={status} value={status}>
            {label(status)}
          </option>
        ))}
      </select>
      <span className={`save-note${state === 'saved' ? ' save-note-visible' : ''}`} aria-live="polite">
        {state === 'saved' ? '✓ Saved' : ''}
      </span>
    </span>
  )
}

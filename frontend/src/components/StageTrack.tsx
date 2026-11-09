import { useEffect, useState } from 'react'
import { pipelineStages } from '../api/types'
import type { ApplicationStatus, PipelineStage } from '../api/types'
import { label } from '../labels'

interface Props {
  status: ApplicationStatus
  furthestStage: PipelineStage | null
  /** Saves a new status; resolves to whether it succeeded. */
  onChange: (status: ApplicationStatus) => Promise<boolean>
}

type SegmentState = 'passed' | 'current' | 'halted' | 'pending'

const closingStatuses = ['Rejected', 'Withdrawn', 'NoResponse'] as const satisfies readonly ApplicationStatus[]

/**
 * The hiring pipeline drawn like a CI pipeline: one segment per stage, filled up to the furthest stage reached.
 * A closed application shows a stopped track at the stage where it ended. Clicking a stage moves the
 * application there; the row underneath closes it (or reopens it as "not applied yet").
 */
export function StageTrack({ status, furthestStage, onChange }: Props) {
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!saved) return
    const timer = setTimeout(() => setSaved(false), 2000)
    return () => clearTimeout(timer)
  }, [saved])

  const reached = furthestStage ? pipelineStages.indexOf(furthestStage) : -1
  const isClosed = (closingStatuses as readonly ApplicationStatus[]).includes(status)
  const isAccepted = status === 'Accepted'

  const segmentState = (index: number): SegmentState => {
    if (index < reached) return 'passed'
    if (index > reached) return 'pending'
    if (isClosed) return 'halted'
    return isAccepted ? 'passed' : 'current'
  }

  const change = async (next: ApplicationStatus) => {
    if (next === status || saving) return
    setSaving(true)
    const ok = await onChange(next)
    setSaving(false)
    setSaved(ok)
  }

  return (
    <div className="stage-track-wrap" aria-busy={saving}>
      <ol className="stage-track" aria-label="Pipeline stage">
        {pipelineStages.map((stage, index) => {
          const state = segmentState(index)
          return (
            <li key={stage} className={`stage stage-${state}`}>
              <button
                type="button"
                aria-pressed={stage === status}
                aria-label={`${label(stage)}${stage === status ? ' (current stage)' : ''}`}
                disabled={saving}
                onClick={() => void change(stage)}
              >
                <span className="stage-bar" />
                <span className="stage-label">{label(stage)}</span>
              </button>
            </li>
          )
        })}
      </ol>

      <div className="stage-footer">
        <p className={`stage-caption${isClosed ? ' stage-caption-halted' : ''}${isAccepted ? ' stage-caption-accepted' : ''}`}>
          {caption(status, furthestStage, isClosed)}
        </p>
        <div className="stage-closers">
          <span className="muted">Mark as</span>
          {closingStatuses.map((closing) => (
            <button
              key={closing}
              type="button"
              className="button-sm button-ghost"
              aria-pressed={status === closing}
              disabled={saving}
              onClick={() => void change(closing)}
            >
              {label(closing)}
            </button>
          ))}
          {(furthestStage === 'Offer' || isAccepted) && (
            <button
              type="button"
              className="button-sm button-ghost"
              aria-pressed={isAccepted}
              disabled={saving}
              onClick={() => void change('Accepted')}
            >
              Accepted
            </button>
          )}
          <button
            type="button"
            className="button-sm button-ghost"
            aria-pressed={status === 'Interested'}
            disabled={saving}
            onClick={() => void change('Interested')}
          >
            Not applied yet
          </button>
          <span className={`save-note${saved ? ' save-note-visible' : ''}`} aria-live="polite">
            {saved ? '✓ Saved' : ''}
          </span>
        </div>
      </div>
    </div>
  )
}

function caption(status: ApplicationStatus, furthest: PipelineStage | null, isClosed: boolean): string {
  if (status === 'Interested') return 'Not applied yet. Click Applied once you send it.'
  if (status === 'Accepted') return 'Offer accepted.'
  if (isClosed) return furthest ? `${label(status)} after ${label(furthest).toLowerCase()}.` : `${label(status)}.`
  return 'Click a stage when the application moves on.'
}

import { useState } from 'react'
import type { SubmitEvent } from 'react'
import { api } from '../../api/client'
import { activityTypes } from '../../api/types'
import type { ActivityType, ApplicationDetail } from '../../api/types'
import { Field, Select } from '../../components/Field'
import { formatDate, label, todayIso } from '../../labels'
import type { Mutate } from './ApplicationDetailPage'

/** 'next' = right after marking done: ask for the following step, starting from an empty form. */
type Mode = 'view' | 'complete' | 'edit' | 'next'

interface Props {
  application: ApplicationDetail
  mutate: Mutate
}

/**
 * The next action has a small lifecycle: set it → do it → mark it done (logged on the timeline) → set the next one.
 * Each step is one click here, instead of going through the full edit form.
 */
export function NextActionSection({ application, mutate }: Props) {
  const [mode, setMode] = useState<Mode>('view')
  const { id, nextActionDescription: description, nextActionDueOn: dueOn } = application

  if (mode === 'complete' && description) {
    return (
      <CompleteForm
        description={description}
        onCancel={() => setMode('view')}
        onComplete={async (type, date) => {
          const ok = await mutate(() => api.completeNextAction(id, { type, date }))
          // Straight into "what's next?", which is the step that usually gets forgotten.
          if (ok) setMode('next')
        }}
      />
    )
  }

  if (mode === 'edit' || mode === 'next') {
    // After completion the page data is still being refreshed, so don't prefill from it.
    const fresh = mode === 'next'
    return (
      <EditForm
        initialDescription={fresh ? '' : (description ?? '')}
        initialDueOn={fresh ? '' : (dueOn ?? '')}
        hasExisting={!fresh && description !== null}
        onCancel={() => setMode('view')}
        onSave={async (newDescription, newDueOn) => {
          const ok = await mutate(() => api.setNextAction(id, { description: newDescription, dueOn: newDueOn }))
          if (ok) setMode('view')
        }}
      />
    )
  }

  return (
    <>
      <h2>Next action</h2>
      {description ? (
        <>
          <div className={dueOn && dueOn <= todayIso() ? 'next-action-card next-action-due' : 'next-action-card'}>
            <p className="next-action-text">{description}</p>
            {dueOn && <p className="next-action-meta">Due {formatDate(dueOn)}</p>}
          </div>
          <div className="next-action-buttons">
            <button type="button" className="button-sm button-soft" onClick={() => setMode('complete')}>
              ✓ Done
            </button>
            <button type="button" className="button-sm button-ghost" onClick={() => setMode('edit')}>
              Change
            </button>
          </div>
        </>
      ) : (
        <p className="muted">
          None set.{' '}
          <button type="button" className="button-link" onClick={() => setMode('edit')}>
            Set next action
          </button>
        </p>
      )}
    </>
  )
}

function CompleteForm({
  description,
  onComplete,
  onCancel,
}: {
  description: string
  onComplete: (type: ActivityType, date: string | null) => Promise<void>
  onCancel: () => void
}) {
  const [type, setType] = useState<ActivityType>('Note')
  const [date, setDate] = useState(todayIso())
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    await onComplete(type, date || null)
    setSaving(false)
  }

  return (
    <form className="next-action-form" onSubmit={handleSubmit}>
      <h2>Mark done</h2>
      <p className="next-action">{description}</p>
      <Field label="Log on the timeline as">
        <Select value={type} options={activityTypes} onChange={(v) => v && setType(v)} formatOption={label} />
      </Field>
      <p className="hint">
        {type === 'Note'
          ? 'A private task. Doesn’t count as contact with the company.'
          : 'Counts as contact: moves “last contact” to this date.'}
      </p>
      <Field label="Date">
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </Field>
      <div className="next-action-buttons">
        <button type="submit" className="button button-primary" disabled={saving}>
          {saving ? 'Saving…' : 'Mark done'}
        </button>
        <button type="button" className="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  )
}

function EditForm({
  initialDescription,
  initialDueOn,
  hasExisting,
  onSave,
  onCancel,
}: {
  initialDescription: string
  initialDueOn: string
  hasExisting: boolean
  onSave: (description: string | null, dueOn: string | null) => Promise<void>
  onCancel: () => void
}) {
  const [description, setDescription] = useState(initialDescription)
  const [dueOn, setDueOn] = useState(initialDueOn)
  const [saving, setSaving] = useState(false)

  const save = async (newDescription: string | null, newDueOn: string | null) => {
    setSaving(true)
    await onSave(newDescription, newDueOn)
    setSaving(false)
  }

  const handleSubmit = (e: SubmitEvent) => {
    e.preventDefault()
    void save(description.trim() || null, dueOn || null)
  }

  return (
    <form className="next-action-form" onSubmit={handleSubmit}>
      <h2>{hasExisting ? 'Change next action' : 'What’s next?'}</h2>
      <Field label="Next action">
        <input value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Follow up with recruiter" autoFocus />
      </Field>
      <Field label="Due on" help="When it's due. Due and overdue actions show up in Follow-ups.">
        <input type="date" value={dueOn} onChange={(e) => setDueOn(e.target.value)} />
      </Field>
      <div className="next-action-buttons">
        <button type="submit" className="button button-primary" disabled={saving || (!description.trim() && !!dueOn)}>
          {saving ? 'Saving…' : 'Save'}
        </button>
        <button type="button" className="button" onClick={onCancel}>
          {hasExisting ? 'Cancel' : 'Skip'}
        </button>
        {hasExisting && (
          <button type="button" className="button-link" onClick={() => void save(null, null)}>
            Remove
          </button>
        )}
      </div>
    </form>
  )
}

import { useState } from 'react'
import type { SubmitEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, api } from '../../api/client'
import { activityTypes } from '../../api/types'
import type { ActivityType, ApplicationDetail, ContactSummary } from '../../api/types'
import { ErrorBanner, Loading } from '../../components/Feedback'
import { Field, Select } from '../../components/Field'
import { StageTrack } from '../../components/StageTrack'
import { useAsync } from '../../hooks/useAsync'
import { formatDate, label, todayIso } from '../../labels'
import { requestWithStatus } from './applicationForm'
import { NextActionSection } from './NextActionSection'

export function ApplicationDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { data: application, error, loading, reload } = useAsync(() => api.getApplication(id), [id])
  const [actionError, setActionError] = useState<unknown>()

  if (loading && !application) return <Loading />
  if (error) {
    return (
      <section>
        <ErrorBanner error={error instanceof ApiError && error.status === 404 ? new Error('Application not found.') : error} />
        <Link to="/">Back to applications</Link>
      </section>
    )
  }
  if (!application) return null

  /** Runs a mutation, then refreshes the page data; errors show in a banner. Resolves to whether it succeeded. */
  const mutate = async (action: () => Promise<unknown>) => {
    setActionError(undefined)
    try {
      await action()
      reload()
      return true
    } catch (err) {
      setActionError(err)
      return false
    }
  }

  const handleDelete = async () => {
    if (!window.confirm(`Delete the ${application.company} application and its timeline?`)) return
    try {
      await api.deleteApplication(application.id)
      navigate('/')
    } catch (err) {
      setActionError(err)
    }
  }

  const location = [application.remoteRegion, application.hybridCity && `Hybrid in ${application.hybridCity}`]
    .filter(Boolean)
    .join(', ')

  return (
    <article className="detail">
      <header className="detail-header">
        <div>
          <Link to="/" className="back-link">
            Applications
          </Link>
          <h1>{application.company}</h1>
          <p className="detail-role">
            {application.roleTitle} · {label(application.channel)} · {label(application.employmentType)}
          </p>
        </div>
        <div className="detail-actions">
          {application.jobUrl && (
            <a className="button-sm button-ghost" href={application.jobUrl} target="_blank" rel="noreferrer">
              Job post ↗
            </a>
          )}
          <Link to={`/applications/${application.id}/edit`} className="button-sm button-bordered">
            Edit
          </Link>
          <button type="button" className="button-sm button-bordered button-bordered-danger" onClick={handleDelete}>
            Delete
          </button>
        </div>
      </header>

      <StageTrack
        status={application.status}
        furthestStage={application.furthestStage}
        onChange={(status) => mutate(() => api.updateApplication(application.id, requestWithStatus(application, status)))}
      />

      <ErrorBanner error={actionError} />

      <div className="detail-body">
        <div className="detail-main">
          <NextActionSection application={application} mutate={mutate} />

          <section className="detail-section">
            <h2>Notes</h2>
            {application.notes ? <p className="notes">{application.notes}</p> : <p className="muted">No notes yet.</p>}
          </section>

          <TimelinePanel application={application} mutate={mutate} />
        </div>

        <aside className="detail-side">
          <dl className="facts">
            <dt>Track</dt>
            <dd>{label(application.track)}</dd>
            <dt>CV sent</dt>
            <dd>{application.cvVersion ?? '—'}</dd>
            <dt>Applied</dt>
            <dd className="tabular">{formatDate(application.appliedOn)}</dd>
            <dt>Last contact</dt>
            <dd className="tabular">{formatDate(application.lastContactOn)}</dd>
            <dt>Location</dt>
            <dd>{location || '—'}</dd>
            <dt>Pay</dt>
            <dd>{formatCompensation(application)}</dd>
          </dl>

          <ContactsPanel application={application} mutate={mutate} />
        </aside>
      </div>
    </article>
  )
}

export type Mutate = (action: () => Promise<unknown>) => Promise<boolean>

function ContactsPanel({ application, mutate }: { application: ApplicationDetail; mutate: Mutate }) {
  const { data: allContacts } = useAsync(() => api.listContacts(), [])
  const [selected, setSelected] = useState('')

  const linkedIds = new Set(application.contacts.map((c) => c.id))
  const available = (allContacts ?? []).filter((c) => !linkedIds.has(c.id))

  return (
    <section className="detail-section">
      <h2>Contacts</h2>
      {application.contacts.length === 0 && <p className="muted">No contacts linked.</p>}
      <ul className="contact-list">
        {application.contacts.map((c: ContactSummary) => (
          <li key={c.id}>
            <span className="contact-name">
              <Link to={`/contacts/${c.id}`}>{c.name}</Link>
              <span className="muted">
                {label(c.role)}
                {c.company && ` at ${c.company}`}
              </span>
            </span>
            <button
              type="button"
              className="button-link"
              onClick={() => mutate(() => api.unlinkContact(application.id, c.id))}
            >
              Unlink
            </button>
          </li>
        ))}
      </ul>

      <div className="inline-form">
        <select aria-label="Contact to link" value={selected} onChange={(e) => setSelected(e.target.value)}>
          <option value="">Link existing contact…</option>
          {available.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
              {c.company ? ` (${c.company})` : ''}
            </option>
          ))}
        </select>
        <button
          type="button"
          className="button"
          disabled={!selected}
          onClick={() => mutate(() => api.linkContact(application.id, selected)).then(() => setSelected(''))}
        >
          Link
        </button>
        <Link to="/contacts/new" className="button-link">
          New contact
        </Link>
      </div>
    </section>
  )
}

function TimelinePanel({ application, mutate }: { application: ApplicationDetail; mutate: Mutate }) {
  const [type, setType] = useState<ActivityType>('MessageSent')
  const [date, setDate] = useState(todayIso())
  const [text, setText] = useState('')

  const handleSubmit = async (e: SubmitEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    await mutate(() => api.addActivity(application.id, { type, text: text.trim(), date: date || null }))
    setText('')
  }

  return (
    <section className="detail-section">
      <h2>Timeline</h2>
      <form className="activity-form" onSubmit={handleSubmit}>
        <Field label="Type" help="What happened. A message, reply, interview or take-home counts as contact; a note is just for you.">
          <Select value={type} options={activityTypes} onChange={(v) => v && setType(v)} formatOption={label} />
        </Field>
        <Field label="Date">
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </Field>
        <Field label="What happened" wide>
          <input
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={type === 'TakeHome' ? 'Build a small REST API with tests' : 'Sent CV to recruiter'}
          />
        </Field>
        <button type="submit" className="button button-primary" disabled={!text.trim()}>
          Add
        </button>
      </form>
      {type === 'TakeHome' && (
        <p className="hint timeline-hint">
          Put the deadline in the next action (for example "Submit take-home", due Friday) so it shows up in Follow-ups.
        </p>
      )}

      {application.activities.length === 0 ? (
        <p className="muted">Nothing logged yet.</p>
      ) : (
        <ol className="timeline">
          {application.activities.map((a) => (
            <li key={a.id} className={`timeline-item timeline-${a.type}`}>
              <span className="timeline-date tabular">{formatDate(a.date)}</span>
              <span className="timeline-type">{label(a.type)}</span>
              <span>{a.text}</span>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

function formatCompensation(a: ApplicationDetail): string {
  if (a.compensationAmount === null) return '—'
  const parts = [
    a.compensationAmount.toLocaleString(),
    a.compensationCurrency,
    a.compensationPeriod && `/ ${a.compensationPeriod.toLowerCase()}`,
    a.compensationIsNet === null ? null : a.compensationIsNet ? '(net)' : '(gross)',
  ]
  return parts.filter(Boolean).join(' ')
}

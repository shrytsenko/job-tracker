import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, api } from '../../api/client'
import { activityTypes } from '../../api/types'
import type { ActivityType, ApplicationDetail, ContactSummary } from '../../api/types'
import { ErrorBanner, Loading } from '../../components/Feedback'
import { Field, Select } from '../../components/Field'
import { StatusSelect } from '../../components/StatusSelect'
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

  const location = [application.remoteRegion, application.hybridCity && `Hybrid · ${application.hybridCity}`]
    .filter(Boolean)
    .join(' / ')

  return (
    <section>
      <div className="page-header">
        <div>
          <p className="muted breadcrumb">
            <Link to="/">Applications</Link> /
          </p>
          <h1>{application.company}</h1>
          <p className="subtitle">
            {application.roleTitle}
            {application.jobUrl && (
              <>
                {' · '}
                <a href={application.jobUrl} target="_blank" rel="noreferrer">
                  Job post ↗
                </a>
              </>
            )}
          </p>
        </div>
        <div className="actions">
          <Link to={`/applications/${application.id}/edit`} className="button">
            Edit
          </Link>
          <button type="button" className="button button-danger" onClick={handleDelete}>
            Delete
          </button>
        </div>
      </div>

      <ErrorBanner error={actionError} />

      <div className="detail-grid">
        <div className="panel">
          <h2>Overview</h2>
          <dl className="facts">
            <dt>Status</dt>
            <dd>
              <StatusSelect
                value={application.status}
                onChange={(status) => mutate(() => api.updateApplication(application.id, requestWithStatus(application, status)))}
              />
            </dd>
            <dt>Track</dt>
            <dd>{label(application.track)}</dd>
            <dt>Channel</dt>
            <dd>{label(application.channel)}</dd>
            <dt>Employment</dt>
            <dd>{label(application.employmentType)}</dd>
            <dt>Location</dt>
            <dd>{location || '—'}</dd>
            <dt>Compensation</dt>
            <dd>{formatCompensation(application)}</dd>
            <dt>Applied on</dt>
            <dd>{formatDate(application.appliedOn)}</dd>
            <dt>Last contact</dt>
            <dd>{formatDate(application.lastContactOn)}</dd>
          </dl>
        </div>

        <div className="panel">
          <NextActionSection application={application} mutate={mutate} />

          <h2>Notes</h2>
          {application.notes ? <p className="notes">{application.notes}</p> : <p className="muted">No notes.</p>}
        </div>

        <ContactsPanel application={application} mutate={mutate} />
        <TimelinePanel application={application} mutate={mutate} />
      </div>
    </section>
  )
}

export type Mutate = (action: () => Promise<unknown>) => Promise<boolean>

function ContactsPanel({ application, mutate }: { application: ApplicationDetail; mutate: Mutate }) {
  const { data: allContacts } = useAsync(() => api.listContacts(), [])
  const [selected, setSelected] = useState('')

  const linkedIds = new Set(application.contacts.map((c) => c.id))
  const available = (allContacts ?? []).filter((c) => !linkedIds.has(c.id))

  return (
    <div className="panel">
      <h2>Contacts</h2>
      {application.contacts.length === 0 && <p className="muted">No contacts linked.</p>}
      <ul className="contact-list">
        {application.contacts.map((c: ContactSummary) => (
          <li key={c.id}>
            <Link to={`/contacts/${c.id}`}>{c.name}</Link>
            <span className="muted">
              {' '}· {label(c.role)}
              {c.company && ` · ${c.company}`}
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
    </div>
  )
}

function TimelinePanel({ application, mutate }: { application: ApplicationDetail; mutate: Mutate }) {
  const [type, setType] = useState<ActivityType>('MessageSent')
  const [date, setDate] = useState(todayIso())
  const [text, setText] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!text.trim()) return
    await mutate(() => api.addActivity(application.id, { type, text: text.trim(), date: date || null }))
    setText('')
  }

  return (
    <div className="panel panel-wide">
      <h2>Timeline</h2>
      <form className="activity-form" onSubmit={handleSubmit}>
        <Field label="Type">
          <Select value={type} options={activityTypes} onChange={(v) => v && setType(v)} formatOption={label} />
        </Field>
        <Field label="Date">
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </Field>
        <Field label="What happened" wide>
          <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Sent CV to recruiter" />
        </Field>
        <button type="submit" className="button button-primary" disabled={!text.trim()}>
          Add
        </button>
      </form>

      {application.activities.length === 0 ? (
        <p className="muted">Nothing logged yet.</p>
      ) : (
        <ol className="timeline">
          {application.activities.map((a) => (
            <li key={a.id} className={`timeline-item timeline-${a.type}`}>
              <span className="timeline-date">{formatDate(a.date)}</span>
              <span className="timeline-type">{label(a.type)}</span>
              <span>{a.text}</span>
            </li>
          ))}
        </ol>
      )}
    </div>
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

import { useState } from 'react'
import type { SubmitEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, api } from '../../api/client'
import { contactRoles } from '../../api/types'
import type { ContactDetail, ContactRole, SaveContactRequest } from '../../api/types'
import { SuggestionInput } from '../../components/SuggestionInput'
import { ErrorBanner, Loading } from '../../components/Feedback'
import { Field, Select } from '../../components/Field'
import { StatusBadge } from '../../components/StatusBadge'
import { useAsync } from '../../hooks/useAsync'
import { label } from '../../labels'

/** Create a contact (/contacts/new) or view and edit one (/contacts/:id). */
export function ContactFormPage() {
  const { id } = useParams()
  const { data, error, loading } = useAsync(() => (id ? api.getContact(id) : Promise.resolve(undefined)), [id])

  if (id && loading) return <Loading />
  if (error) return <ErrorBanner error={error} />

  return <ContactForm key={id ?? 'new'} existing={data} />
}

interface ContactFormValues {
  name: string
  role: ContactRole
  company: string
  linkedInUrl: string
  email: string
  notes: string
  lastContactOn: string
}

const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

function ContactForm({ existing }: { existing?: ContactDetail }) {
  const navigate = useNavigate()
  const [values, setValues] = useState<ContactFormValues>({
    name: existing?.name ?? '',
    role: existing?.role ?? 'Recruiter',
    company: existing?.company ?? '',
    linkedInUrl: existing?.linkedInUrl ?? '',
    email: existing?.email ?? '',
    notes: existing?.notes ?? '',
    lastContactOn: existing?.lastContactOn ?? '',
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>()

  const set = <K extends keyof ContactFormValues>(key: K, value: ContactFormValues[K]) =>
    setValues((current) => ({ ...current, [key]: value }))
  const fieldErrors = error instanceof ApiError ? error.fieldErrors : {}

  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    const request: SaveContactRequest = {
      name: values.name.trim(),
      role: values.role,
      company: orNull(values.company),
      linkedInUrl: orNull(values.linkedInUrl),
      email: orNull(values.email),
      notes: orNull(values.notes),
      lastContactOn: values.lastContactOn || null,
    }
    try {
      if (existing) {
        await api.updateContact(existing.id, request)
      } else {
        await api.createContact(request)
      }
      navigate('/contacts')
    } catch (err) {
      setError(err)
      setSaving(false)
    }
  }

  async function handleDelete() {
    if (!existing || !window.confirm(`Delete ${existing.name}? They will be unlinked from all applications.`)) return
    try {
      await api.deleteContact(existing.id)
      navigate('/contacts')
    } catch (err) {
      setError(err)
    }
  }

  return (
    <section>
      <div className="page-header">
        <div>
          <p className="muted breadcrumb">
            <Link to="/contacts">Contacts</Link> /
          </p>
          <h1>{existing ? existing.name : 'New contact'}</h1>
        </div>
        {existing && (
          <button type="button" className="button button-danger" onClick={handleDelete}>
            Delete
          </button>
        )}
      </div>

      <form className="form" onSubmit={handleSubmit} noValidate>
        <ErrorBanner error={error} />
        <fieldset>
          <legend>Contact</legend>
          <Field label="Name *" errors={fieldErrors.name}>
            <input value={values.name} onChange={(e) => set('name', e.target.value)} required autoFocus={!existing} />
          </Field>
          <Field label="Role">
            <Select value={values.role} options={contactRoles} onChange={(v) => v && set('role', v)} formatOption={label} />
          </Field>
          <Field label="Company" help="Where they work. For agency recruiters, this is the agency, not the hiring company." errors={fieldErrors.company}>
            <SuggestionInput value={values.company} onChange={(v) => set('company', v)} loadSuggestions={api.listCompanies} />
          </Field>
          <Field label="Email" errors={fieldErrors.email}>
            <input type="email" value={values.email} onChange={(e) => set('email', e.target.value)} />
          </Field>
          <Field label="LinkedIn URL" errors={fieldErrors.linkedInUrl} wide>
            <input type="url" value={values.linkedInUrl} onChange={(e) => set('linkedInUrl', e.target.value)} placeholder="https://www.linkedin.com/in/…" />
          </Field>
          <Field label="Last contact on">
            <input type="date" value={values.lastContactOn} onChange={(e) => set('lastContactOn', e.target.value)} />
          </Field>
          <Field label="Notes" errors={fieldErrors.notes} wide>
            <textarea rows={4} value={values.notes} onChange={(e) => set('notes', e.target.value)} />
          </Field>
        </fieldset>

        <div className="form-actions">
          <button type="submit" className="button button-primary" disabled={saving}>
            {saving ? 'Saving…' : existing ? 'Save changes' : 'Create contact'}
          </button>
          <Link to="/contacts" className="button">
            Cancel
          </Link>
        </div>
      </form>

      {existing && (
        <div className="panel">
          <h2>Linked applications</h2>
          {existing.applications.length === 0 ? (
            <p className="muted">Not linked to any application. Link from an application's page.</p>
          ) : (
            <ul className="contact-list">
              {existing.applications.map((a) => (
                <li key={a.id}>
                  <Link to={`/applications/${a.id}`}>{a.company}</Link>
                  <span className="muted"> · {a.roleTitle} </span>
                  <StatusBadge status={a.status} />
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </section>
  )
}

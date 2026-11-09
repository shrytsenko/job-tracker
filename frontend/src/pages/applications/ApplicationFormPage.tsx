import { useState } from 'react'
import type { SubmitEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, api } from '../../api/client'
import { applicationStatuses, channels, compensationPeriods, currencies, employmentTypes, tracks } from '../../api/types'
import type { ApplicationDetail } from '../../api/types'
import { SuggestionInput } from '../../components/SuggestionInput'
import { ErrorBanner, Loading } from '../../components/Feedback'
import { Field, Select } from '../../components/Field'
import { useAsync } from '../../hooks/useAsync'
import { label } from '../../labels'
import { emptyApplicationForm, formFromDetail, requestFromForm } from './applicationForm'
import type { ApplicationFormValues } from './applicationForm'

/** Route wrapper: loads the application when editing, then renders the form. */
export function ApplicationFormPage() {
  const { id } = useParams()
  const { data, error, loading } = useAsync(
    () => (id ? api.getApplication(id) : Promise.resolve(undefined)),
    [id],
  )

  if (id && loading) return <Loading />
  if (error) return <ErrorBanner error={error} />

  // `key` resets the form state if we navigate from editing one application to another.
  return <ApplicationForm key={id ?? 'new'} existing={data} />
}

function ApplicationForm({ existing }: { existing?: ApplicationDetail }) {
  const navigate = useNavigate()
  const [values, setValues] = useState<ApplicationFormValues>(
    existing ? formFromDetail(existing) : emptyApplicationForm,
  )
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>()

  const set = <K extends keyof ApplicationFormValues>(key: K, value: ApplicationFormValues[K]) =>
    setValues((current) => ({ ...current, [key]: value }))

  const fieldErrors = error instanceof ApiError ? error.fieldErrors : {}

  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    try {
      const request = requestFromForm(values)
      const saved = existing
        ? await api.updateApplication(existing.id, request)
        : await api.createApplication(request)
      navigate(`/applications/${saved.id}`)
    } catch (err) {
      setError(err)
      setSaving(false)
    }
  }

  return (
    <section>
      <div className="page-header">
        <h1>{existing ? `Edit ${existing.company}` : 'New application'}</h1>
      </div>

      <form className="form" onSubmit={handleSubmit} noValidate>
        <ErrorBanner error={error} />

        <fieldset>
          <legend>Role</legend>
          <Field label="Company *" help="The company you would work for. For agency roles, use the end client if you know it, otherwise the agency." errors={fieldErrors.company}>
            <SuggestionInput value={values.company} onChange={(v) => set('company', v)} loadSuggestions={api.listCompanies} autoFocus />
          </Field>
          <Field label="Role title *" errors={fieldErrors.roleTitle}>
            <input value={values.roleTitle} onChange={(e) => set('roleTitle', e.target.value)} required />
          </Field>
          <Field label="Job URL" errors={fieldErrors.jobUrl} wide>
            <input type="url" value={values.jobUrl} onChange={(e) => set('jobUrl', e.target.value)} placeholder="https://…" />
          </Field>
          <Field label="CV sent" help="Which version of your CV you sent. Reuse the same names so you can see which CV gets replies." errors={fieldErrors.cvVersion}>
            <SuggestionInput value={values.cvVersion} onChange={(v) => set('cvVersion', v)} loadSuggestions={api.listCvVersions} placeholder="Developer CV v3" />
          </Field>
          <Field label="Status">
            <Select value={values.status} options={applicationStatuses} onChange={(v) => v && set('status', v)} formatOption={label} />
          </Field>
          <Field label="Track" help="Which of your job searches this belongs to. The dashboard compares tracks to show where to focus.">
            <Select value={values.track} options={tracks} onChange={(v) => v && set('track', v)} formatOption={label} />
          </Field>
          <Field label="Channel">
            <Select value={values.channel} options={channels} onChange={(v) => v && set('channel', v)} formatOption={label} />
          </Field>
          <Field label="Employment type">
            <Select value={values.employmentType} options={employmentTypes} onChange={(v) => v && set('employmentType', v)} formatOption={label} />
          </Field>
        </fieldset>

        <fieldset>
          <legend>Location</legend>
          <Field label="Remote region" help="Where you can work from, as the post says. For example Remote EU or US only." errors={fieldErrors.remoteRegion}>
            <input value={values.remoteRegion} onChange={(e) => set('remoteRegion', e.target.value)} placeholder="Remote EU, US only…" />
          </Field>
          <Field label="Hybrid city" errors={fieldErrors.hybridCity}>
            <input value={values.hybridCity} onChange={(e) => set('hybridCity', e.target.value)} placeholder="Barcelona" />
          </Field>
        </fieldset>

        <fieldset>
          <legend>Compensation</legend>
          <Field label="Amount" errors={fieldErrors.compensationAmount}>
            <input type="number" min="0" step="any" value={values.compensationAmount} onChange={(e) => set('compensationAmount', e.target.value)} />
          </Field>
          <Field label="Currency" errors={fieldErrors.compensationCurrency}>
            <Select value={values.compensationCurrency} options={currencies} onChange={(v) => set('compensationCurrency', v)} placeholder="—" />
          </Field>
          <Field label="Per">
            <Select value={values.compensationPeriod} options={compensationPeriods} onChange={(v) => set('compensationPeriod', v)} placeholder="—" formatOption={label} />
          </Field>
          <Field label="Net / gross">
            <Select value={values.compensationBasis} options={['net', 'gross'] as const} onChange={(v) => set('compensationBasis', v)} placeholder="—" formatOption={label} />
          </Field>
        </fieldset>

        <fieldset>
          <legend>Dates and next step</legend>
          <Field label="Applied on" help="The day you sent the application. Filled in for you when the status changes to Applied.">
            <input type="date" value={values.appliedOn} onChange={(e) => set('appliedOn', e.target.value)} />
          </Field>
          <Field label="Last contact on" help="The last time you and the company were in touch. Logging a message, reply, interview or take-home updates it for you.">
            <input type="date" value={values.lastContactOn} onChange={(e) => set('lastContactOn', e.target.value)} />
          </Field>
          <Field label="Next action" errors={fieldErrors.nextActionDescription}>
            <input value={values.nextActionDescription} onChange={(e) => set('nextActionDescription', e.target.value)} placeholder="Follow up with recruiter" />
          </Field>
          <Field label="Due on" help="When the next action is due. Due and overdue actions show up in Follow-ups.">
            <input type="date" value={values.nextActionDueOn} onChange={(e) => set('nextActionDueOn', e.target.value)} />
          </Field>
        </fieldset>

        <fieldset>
          <legend>Notes</legend>
          <Field label="Notes" errors={fieldErrors.notes} wide>
            <textarea rows={6} value={values.notes} onChange={(e) => set('notes', e.target.value)} />
          </Field>
        </fieldset>

        <div className="form-actions">
          <button type="submit" className="button button-primary" disabled={saving}>
            {saving ? 'Saving…' : existing ? 'Save changes' : 'Create application'}
          </button>
          <Link to={existing ? `/applications/${existing.id}` : '/'} className="button">
            Cancel
          </Link>
        </div>
      </form>
    </section>
  )
}

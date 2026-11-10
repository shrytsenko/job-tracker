import { useEffect, useState } from 'react'
import type { SubmitEvent } from 'react'
import { api } from '../api/client'
import type { Profile } from '../api/types'
import { ErrorBanner, Loading } from '../components/Feedback'
import { useAsync } from '../hooks/useAsync'
import { formatDate } from '../labels'

const MAX_LENGTH = 8000

export function ProfilePage() {
  const { data, error, loading } = useAsync(() => api.getProfile(), [])

  if (loading && !data) return <Loading />
  if (error) return <ErrorBanner error={error} />
  if (!data) return null

  return <ProfileForm profile={data} />
}

function ProfileForm({ profile }: { profile: Profile }) {
  const [cvSummary, setCvSummary] = useState(profile.cvSummary)
  const [savedAt, setSavedAt] = useState(profile.updatedAt)
  const [saving, setSaving] = useState(false)
  const [justSaved, setJustSaved] = useState(false)
  const [error, setError] = useState<unknown>()

  useEffect(() => {
    if (!justSaved) return
    const timer = setTimeout(() => setJustSaved(false), 2500)
    return () => clearTimeout(timer)
  }, [justSaved])

  const handleSubmit = async (e: SubmitEvent) => {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    try {
      const saved = await api.saveProfile(cvSummary)
      setCvSummary(saved.cvSummary)
      setSavedAt(saved.updatedAt)
      setJustSaved(true)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="profile">
      <div className="page-header">
        <div>
          <h1>Profile</h1>
          <p className="subtitle">Job analysis compares every job post with this summary, so it drives the match score and red flags.</p>
        </div>
      </div>

      <form className="profile-form" onSubmit={handleSubmit}>
        <ErrorBanner error={error} />

        <label className="field">
          <span className="field-label">CV summary</span>
          <textarea
            rows={16}
            maxLength={MAX_LENGTH}
            value={cvSummary}
            onChange={(e) => setCvSummary(e.target.value)}
            placeholder={'Senior .NET developer, 12 years in IT. C#, ASP.NET Core, EF Core, SQL Server, Azure. 5 years of test automation (Playwright, NUnit). Based in Barcelona, EU citizen, open to remote EU/UK or hybrid in Barcelona. English C1, Spanish B2.'}
          />
          <span className="field-hint">
            {cvSummary.length.toLocaleString()} / {MAX_LENGTH.toLocaleString()} characters
          </span>
        </label>

        <div className="profile-tips">
          <h2>What to include</h2>
          <ul>
            <li>Your main stack and years of experience with each part of it.</li>
            <li>Where you live and where you are allowed to work. Restrictions like &ldquo;US only&rdquo; are flagged against this.</li>
            <li>Languages you speak, and the level.</li>
            <li>What you&rsquo;re looking for: role types, seniority, remote or on-site, contract or permanent.</li>
          </ul>
          <p className="muted">It stays in your local database and is sent only to the AI provider, only when you analyse a job post.</p>
        </div>

        <div className="form-actions">
          <button type="submit" className="button button-primary" disabled={saving}>
            {saving ? 'Saving…' : 'Save profile'}
          </button>
          <span className={`save-note${justSaved ? ' save-note-visible' : ''}`} aria-live="polite">
            {justSaved ? '✓ Saved' : ''}
          </span>
          {savedAt && !justSaved && <span className="muted">Last saved {formatDate(savedAt.slice(0, 10))}</span>}
        </div>
      </form>
    </section>
  )
}

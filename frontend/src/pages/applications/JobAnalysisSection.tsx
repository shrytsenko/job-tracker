import { useState } from 'react'
import type { SubmitEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, api } from '../../api/client'
import type { ApplicationDetail, JobAnalysis } from '../../api/types'
import { formatDate } from '../../labels'

interface Props {
  application: ApplicationDetail
  /** Reloads the page data after a successful analysis. */
  onAnalyzed: () => void
}

/**
 * Paste a job post, get the AI's reading of it against your CV summary. Red flags come first and loudest:
 * they are the reason to analyse a post at all.
 */
export function JobAnalysisSection({ application, onAnalyzed }: Props) {
  const { analysis, jobDescription } = application
  const [editing, setEditing] = useState(false)
  const [text, setText] = useState(jobDescription ?? '')
  const [analyzing, setAnalyzing] = useState(false)
  const [error, setError] = useState<unknown>()

  const run = async (newText: string | null) => {
    setAnalyzing(true)
    setError(undefined)
    try {
      await api.analyzeJob(application.id, newText)
      setEditing(false)
      onAnalyzed()
    } catch (err) {
      setError(err)
    } finally {
      setAnalyzing(false)
    }
  }

  const handleSubmit = (e: SubmitEvent) => {
    e.preventDefault()
    if (text.trim()) void run(text)
  }

  const showForm = editing || !analysis

  return (
    <section className="detail-section analysis" aria-busy={analyzing}>
      <h2>Job analysis</h2>

      <AnalysisError error={error} />

      {showForm ? (
        <form className="analysis-form" onSubmit={handleSubmit}>
          {!analysis && !jobDescription && (
            <p className="muted analysis-intro">
              Paste the job post to see the stack it asks for, how well it matches your CV, and any red flags such as
              location or work-permit restrictions.
            </p>
          )}
          <textarea
            aria-label="Job post text"
            rows={editing ? 12 : 8}
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder="Paste the full job post here…"
            disabled={analyzing}
          />
          <div className="next-action-buttons">
            <button type="submit" className="button button-primary" disabled={analyzing || !text.trim()}>
              {analyzing ? 'Analysing…' : 'Analyse job post'}
            </button>
            {editing && (
              <button type="button" className="button-sm button-ghost" disabled={analyzing} onClick={() => setEditing(false)}>
                Cancel
              </button>
            )}
            {analyzing && <span className="muted">This usually takes 10–60 seconds.</span>}
          </div>
        </form>
      ) : (
        <AnalysisResult
          analysis={analysis}
          analyzing={analyzing}
          onReanalyze={() => void run(null)}
          onEdit={() => {
            setText(jobDescription ?? '')
            setEditing(true)
          }}
        />
      )}
    </section>
  )
}

function AnalysisResult({
  analysis,
  analyzing,
  onReanalyze,
  onEdit,
}: {
  analysis: JobAnalysis
  analyzing: boolean
  onReanalyze: () => void
  onEdit: () => void
}) {
  const tone = analysis.matchScore >= 70 ? 'good' : analysis.matchScore >= 40 ? 'fair' : 'low'

  return (
    <div className="analysis-result">
      {analysis.redFlags.length > 0 && (
        <div className="red-flags" role="alert">
          <h3>
            <span aria-hidden="true">⚠</span> {analysis.redFlags.length === 1 ? 'Red flag' : `${analysis.redFlags.length} red flags`}
          </h3>
          <ul>
            {analysis.redFlags.map((flag) => (
              <li key={flag}>{flag}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="analysis-summary">
        <div className={`match-score match-${tone}`}>
          <span className="match-value">{analysis.matchScore}</span>
          <span className="match-label">match with your CV, out of 100</span>
          <span className="match-bar" aria-hidden="true">
            <span style={{ width: `${analysis.matchScore}%` }} />
          </span>
        </div>

        <dl className="facts analysis-facts">
          <dt>Seniority</dt>
          <dd>{analysis.seniority ?? '—'}</dd>
          <dt>Location</dt>
          <dd>{analysis.locationRestrictions ?? '—'}</dd>
          <dt>Employment</dt>
          <dd>{analysis.employmentType ?? '—'}</dd>
          <dt>Salary</dt>
          <dd>{analysis.salary ?? 'Not stated'}</dd>
        </dl>
      </div>

      {analysis.stack.length > 0 && (
        <ul className="stack-list" aria-label="Stack">
          {analysis.stack.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      )}

      <div className="fit-columns">
        <FitList title="Strengths" items={analysis.strengths} empty="None found." className="fit-strengths" />
        <FitList title="Gaps" items={analysis.gaps} empty="None found." className="fit-gaps" />
      </div>

      <div className="analysis-footer">
        <span className="muted">
          Analysed {formatDate(analysis.analyzedAt.slice(0, 10))} with {analysis.model}
        </span>
        <span className="next-action-buttons">
          <button type="button" className="button-sm button-soft" disabled={analyzing} onClick={onReanalyze}>
            {analyzing ? 'Analysing…' : 'Re-analyse'}
          </button>
          <button type="button" className="button-sm button-ghost" disabled={analyzing} onClick={onEdit}>
            Edit job post
          </button>
        </span>
      </div>
    </div>
  )
}

function FitList({ title, items, empty, className }: { title: string; items: string[]; empty: string; className: string }) {
  return (
    <div className={`fit-list ${className}`}>
      <h3>{title}</h3>
      {items.length === 0 ? (
        <p className="muted">{empty}</p>
      ) : (
        <ul>
          {items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

/** Errors in the section's own voice; the missing-CV case links straight to the fix. */
function AnalysisError({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof Error ? error.message : String(error)
  const needsProfile = error instanceof ApiError && error.status === 409

  return (
    <div className="error-banner" role="alert">
      {message} {needsProfile && <Link to="/profile">Open Profile</Link>}
    </div>
  )
}

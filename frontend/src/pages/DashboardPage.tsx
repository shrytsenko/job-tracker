import { Link } from 'react-router'
import { api } from '../api/client'
import type { ApplicationStatus, DashboardStats, TrackFunnel } from '../api/types'
import { BarList } from '../components/BarList'
import type { BarRow } from '../components/BarList'
import { ErrorBanner, Loading } from '../components/Feedback'
import { useAsync } from '../hooks/useAsync'
import { label } from '../labels'

const inProgress: ApplicationStatus[] = ['Applied', 'RecruiterScreen', 'TechInterview', 'FinalInterview', 'Offer']

/** Funnel step names as questions answered by the bar: "got a recruiter screen". */
const reachedPhrase: Partial<Record<ApplicationStatus, string>> = {
  Applied: 'applied',
  RecruiterScreen: 'got a recruiter screen',
  TechInterview: 'got a tech interview',
  Offer: 'got an offer',
}

const percent = (ratio: number | null) => (ratio === null ? null : `${Math.round(ratio * 100)}%`)

export function DashboardPage() {
  const { data, error, loading } = useAsync(() => api.getDashboard(), [])

  if (loading && !data) return <Loading />
  if (error) return <ErrorBanner error={error} />
  if (!data) return null

  return (
    <section className="dashboard">
      <div className="page-header">
        <h1>Dashboard</h1>
      </div>

      {data.total === 0 ? (
        <p className="empty">
          No applications yet, so there is nothing to count. <Link to="/applications/new">Add your first one</Link>.
        </p>
      ) : (
        <Dashboard stats={data} />
      )}
    </section>
  )
}

function Dashboard({ stats }: { stats: DashboardStats }) {
  const count = (status: ApplicationStatus) => stats.byStatus.find((s) => s.status === status)?.count ?? 0
  const applied = stats.funnels.reduce((sum, f) => sum + f.steps[0].reached, 0)
  const active = inProgress.reduce((sum, s) => sum + count(s), 0)
  const offers = count('Offer') + count('Accepted')

  // Developer and SDET are always compared (the 30-day decision); other tracks appear once they have data.
  const funnels = stats.funnels.filter((f) => f.track === 'Developer' || f.track === 'SDET' || f.total > 0)
  // One scale for every panel, so a longer bar always means more applications.
  const funnelMax = Math.max(1, ...funnels.map((f) => f.steps[0].reached))

  return (
    <>
      <dl className="kpi-row">
        <div className="kpi">
          <dt>Tracked</dt>
          <dd>{stats.total}</dd>
        </div>
        <div className="kpi">
          <dt>Sent</dt>
          <dd>{applied}</dd>
        </div>
        <div className="kpi">
          <dt>In progress</dt>
          <dd>{active}</dd>
        </div>
        <div className="kpi">
          <dt>Offers</dt>
          <dd>{offers}</dd>
        </div>
      </dl>

      <h2 className="dashboard-heading">Funnel by track</h2>
      <p className="muted dashboard-intro">
        Each step counts applications that got at least that far, including ones rejected later.
      </p>
      <div className="funnel-grid">
        {funnels.map((funnel) => (
          <FunnelPanel key={funnel.track} funnel={funnel} max={funnelMax} />
        ))}
      </div>

      <ConversionTable funnels={funnels} />

      <h2 className="dashboard-heading">By status</h2>
      <div className="status-panel">
        <BarList
          label="Applications by status"
          max={Math.max(1, ...stats.byStatus.map((s) => s.count))}
          rows={stats.byStatus.map((s) => ({
            key: s.status,
            label: label(s.status),
            value: s.count,
            description: `${label(s.status)}: ${s.count} ${s.count === 1 ? 'application' : 'applications'}`,
          }))}
        />
      </div>
    </>
  )
}

function FunnelPanel({ funnel, max }: { funnel: TrackFunnel; max: number }) {
  const applied = funnel.steps[0].reached

  const rows: BarRow[] = funnel.steps.map((step, index) => {
    const conversion = percent(step.conversionFromPrevious)
    const previous = index > 0 ? funnel.steps[index - 1] : null
    const description = previous
      ? `${step.reached} of ${previous.reached} ${reachedPhrase[step.stage]}${conversion ? ` (${conversion})` : ''}`
      : `${step.reached} ${step.reached === 1 ? 'application' : 'applications'} sent`
    return { key: step.stage, label: label(step.stage), value: step.reached, note: conversion ?? undefined, description }
  })

  const offers = funnel.steps[funnel.steps.length - 1].reached
  const summary =
    applied === 0
      ? 'Nothing applied to yet.'
      : `${offers} ${offers === 1 ? 'offer' : 'offers'} from ${applied} applied (${percent(funnel.overallConversion)}).`

  return (
    <section className="funnel-panel" aria-label={`${label(funnel.track)} funnel`}>
      <h3>{label(funnel.track)}</h3>
      <p className="funnel-summary">{summary}</p>
      <BarList label={`${label(funnel.track)} funnel`} rows={rows} max={max} />
    </section>
  )
}

/** The same numbers as a table: exact figures side by side, and the accessible view of the funnel bars. */
function ConversionTable({ funnels }: { funnels: TrackFunnel[] }) {
  const stages = funnels[0]?.steps.map((s) => s.stage) ?? []

  return (
    <table className="table conversion-table">
      <caption>Conversion from the previous step</caption>
      <thead>
        <tr>
          <th scope="col">Step</th>
          {funnels.map((f) => (
            <th key={f.track} scope="col">
              {label(f.track)}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {stages.map((stage, index) => (
          <tr key={stage}>
            <th scope="row">{label(stage)}</th>
            {funnels.map((f) => {
              const step = f.steps[index]
              return (
                <td key={f.track} className="tabular">
                  {step.reached}
                  {step.conversionFromPrevious !== null && (
                    <span className="muted"> ({percent(step.conversionFromPrevious)})</span>
                  )}
                </td>
              )
            })}
          </tr>
        ))}
      </tbody>
    </table>
  )
}

import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import { ErrorBanner, Loading } from '../components/Feedback'
import { StatusBadge } from '../components/StatusBadge'
import { useAsync } from '../hooks/useAsync'
import { formatDate } from '../labels'

const DEFAULT_DAYS = 5

export function FollowUpsPage() {
  const [params, setParams] = useSearchParams()
  const parsed = Number(params.get('days'))
  const days = Number.isInteger(parsed) && parsed >= 1 && parsed <= 365 ? parsed : DEFAULT_DAYS

  const { data, error, loading } = useAsync(() => api.getFollowUps(days), [days])

  return (
    <section>
      <div className="page-header">
        <div>
          <h1>Needs follow-up</h1>
          <p className="subtitle">
            In-progress applications with no contact for{' '}
            <input
              className="inline-number"
              type="number"
              min={1}
              max={365}
              aria-label="Days without contact"
              value={days}
              onChange={(e) => setParams(e.target.value ? { days: e.target.value } : {}, { replace: true })}
            />{' '}
            days, or a next action due today or earlier.
          </p>
        </div>
      </div>

      <ErrorBanner error={error} />
      {loading && !data ? (
        <Loading />
      ) : data && data.length === 0 ? (
        <p className="empty">Nothing to chase. You're up to date.</p>
      ) : (
        data && (
          <table className="table">
            <thead>
              <tr>
                <th>Company</th>
                <th>Role</th>
                <th>Status</th>
                <th>Why</th>
                <th>Next action</th>
              </tr>
            </thead>
            <tbody>
              {data.map(({ application: a, daysSinceContact, isStale, isNextActionDue }) => (
                <tr key={a.id}>
                  <td>
                    <Link to={`/applications/${a.id}`}>{a.company}</Link>
                  </td>
                  <td>{a.roleTitle}</td>
                  <td>
                    <StatusBadge status={a.status} />
                  </td>
                  <td className="reasons">
                    {isNextActionDue && <span className="reason reason-due">Action due</span>}
                    {isStale && <span className="reason reason-stale">No contact for {daysSinceContact} days</span>}
                  </td>
                  <td>
                    {a.nextActionDescription ?? <span className="muted">—</span>}
                    {a.nextActionDueOn && <span className="muted"> · {formatDate(a.nextActionDueOn)}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )
      )}
    </section>
  )
}

import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../../api/client'
import { applicationStatuses, channels, tracks } from '../../api/types'
import type { ApplicationFilters, ApplicationStatus, Channel, Track } from '../../api/types'
import { ErrorBanner, Loading } from '../../components/Feedback'
import { Select } from '../../components/Field'
import { StatusBadge } from '../../components/StatusBadge'
import { useAsync } from '../../hooks/useAsync'
import { formatDate, label } from '../../labels'

export function ApplicationsPage() {
  // Filters live in the URL, so reload, back/forward and bookmarks keep them.
  const [params, setParams] = useSearchParams()
  const filters: ApplicationFilters = {
    status: (params.get('status') as ApplicationStatus) || undefined,
    track: (params.get('track') as Track) || undefined,
    channel: (params.get('channel') as Channel) || undefined,
    search: params.get('search') || undefined,
  }

  const setFilter = (key: keyof ApplicationFilters, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  // Search typing updates local state immediately and the URL (and so the request) after a short pause.
  const [searchText, setSearchText] = useState(filters.search ?? '')
  useEffect(() => {
    const timer = setTimeout(() => {
      if (searchText.trim() !== (filters.search ?? '')) setFilter('search', searchText.trim())
    }, 300)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only react to the typed text
  }, [searchText])

  const { data, error, loading } = useAsync(
    () => api.listApplications(filters),
    [filters.status, filters.track, filters.channel, filters.search],
  )

  const hasFilters = params.size > 0

  return (
    <section>
      <div className="page-header">
        <h1>Applications</h1>
        <Link to="/applications/new" className="button button-primary">
          New application
        </Link>
      </div>

      <div className="filters">
        <input
          type="search"
          placeholder="Search company or role…"
          aria-label="Search company or role"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <Select
          name="status"
          value={filters.status ?? ''}
          options={applicationStatuses}
          onChange={(v) => setFilter('status', v)}
          placeholder="All statuses"
          formatOption={label}
        />
        <Select
          name="track"
          value={filters.track ?? ''}
          options={tracks}
          onChange={(v) => setFilter('track', v)}
          placeholder="All tracks"
          formatOption={label}
        />
        <Select
          name="channel"
          value={filters.channel ?? ''}
          options={channels}
          onChange={(v) => setFilter('channel', v)}
          placeholder="All channels"
          formatOption={label}
        />
        {hasFilters && (
          <button
            type="button"
            className="button-link"
            onClick={() => {
              setSearchText('')
              setParams({}, { replace: true })
            }}
          >
            Clear filters
          </button>
        )}
      </div>

      <ErrorBanner error={error} />
      {loading && !data ? (
        <Loading />
      ) : data && data.length === 0 ? (
        <p className="empty">
          {hasFilters ? 'No applications match these filters.' : 'No applications yet. Add your first one.'}
        </p>
      ) : (
        data && (
          <table className="table">
            <thead>
              <tr>
                <th>Company</th>
                <th>Role</th>
                <th>Status</th>
                <th>Track</th>
                <th>Channel</th>
                <th>Last contact</th>
                <th>Next action</th>
              </tr>
            </thead>
            <tbody>
              {data.map((a) => (
                <tr key={a.id}>
                  <td>
                    <Link to={`/applications/${a.id}`}>{a.company}</Link>
                  </td>
                  <td>{a.roleTitle}</td>
                  <td>
                    <StatusBadge status={a.status} />
                  </td>
                  <td>{label(a.track)}</td>
                  <td>{label(a.channel)}</td>
                  <td>{formatDate(a.lastContactOn)}</td>
                  <td>
                    {a.nextActionDescription ? (
                      <>
                        {a.nextActionDescription}
                        {a.nextActionDueOn && <span className="muted"> · {formatDate(a.nextActionDueOn)}</span>}
                      </>
                    ) : (
                      <span className="muted">—</span>
                    )}
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

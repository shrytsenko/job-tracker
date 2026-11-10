import { NavLink, Outlet, useLocation } from 'react-router'
import { api } from '../api/client'
import { useAsync } from '../hooks/useAsync'

export function Layout() {
  // Re-checked on every page change, so the count catches up after you log activity or edit an application.
  const { pathname } = useLocation()
  const { data: followUps } = useAsync(() => api.getFollowUps(5), [pathname])
  const followUpCount = followUps?.length ?? 0

  return (
    <div className="app">
      <header className="topbar">
        <span className="brand">Job Tracker</span>
        <nav>
          <NavLink to="/" end>
            Applications
          </NavLink>
          <NavLink to="/follow-ups">
            Follow-ups
            {followUpCount > 0 && (
              <span className="nav-count" aria-label={`${followUpCount} need follow-up`}>
                {followUpCount}
              </span>
            )}
          </NavLink>
          <NavLink to="/dashboard">Dashboard</NavLink>
          <NavLink to="/contacts">Contacts</NavLink>
        </nav>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}

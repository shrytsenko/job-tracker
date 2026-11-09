import { Link } from 'react-router'
import { api } from '../../api/client'
import { ErrorBanner, Loading } from '../../components/Feedback'
import { useAsync } from '../../hooks/useAsync'
import { label } from '../../labels'

export function ContactsPage() {
  const { data, error, loading } = useAsync(() => api.listContacts(), [])

  return (
    <section>
      <div className="page-header">
        <h1>Contacts</h1>
        <Link to="/contacts/new" className="button button-primary">
          New contact
        </Link>
      </div>

      <ErrorBanner error={error} />
      {loading && !data ? (
        <Loading />
      ) : data && data.length === 0 ? (
        <p className="empty">No contacts yet. Add recruiters, referrers and hiring managers here.</p>
      ) : (
        data && (
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Company</th>
              </tr>
            </thead>
            <tbody>
              {data.map((c) => (
                <tr key={c.id}>
                  <td>
                    <Link to={`/contacts/${c.id}`}>{c.name}</Link>
                  </td>
                  <td>{label(c.role)}</td>
                  <td>{c.company ?? <span className="muted">—</span>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )
      )}
    </section>
  )
}

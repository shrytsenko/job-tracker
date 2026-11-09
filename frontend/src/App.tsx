import { Link, Route, Routes } from 'react-router'
import { Layout } from './components/Layout'
import { FollowUpsPage } from './pages/FollowUpsPage'
import { ApplicationDetailPage } from './pages/applications/ApplicationDetailPage'
import { ApplicationFormPage } from './pages/applications/ApplicationFormPage'
import { ApplicationsPage } from './pages/applications/ApplicationsPage'
import { ContactFormPage } from './pages/contacts/ContactFormPage'
import { ContactsPage } from './pages/contacts/ContactsPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ApplicationsPage />} />
        <Route path="applications/new" element={<ApplicationFormPage />} />
        <Route path="applications/:id" element={<ApplicationDetailPage />} />
        <Route path="applications/:id/edit" element={<ApplicationFormPage />} />
        <Route path="follow-ups" element={<FollowUpsPage />} />
        <Route path="contacts" element={<ContactsPage />} />
        <Route path="contacts/new" element={<ContactFormPage />} />
        <Route path="contacts/:id" element={<ContactFormPage />} />
        <Route
          path="*"
          element={
            <p>
              Page not found. <Link to="/">Back to applications</Link>
            </p>
          }
        />
      </Route>
    </Routes>
  )
}

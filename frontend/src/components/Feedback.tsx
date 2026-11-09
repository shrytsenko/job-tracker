import { ApiError } from '../api/client'

/** Shows a request error. Field-level validation messages are shown by the form fields instead. */
export function ErrorBanner({ error }: { error: unknown }) {
  if (!error) return null

  const message =
    error instanceof ApiError && Object.keys(error.fieldErrors).length > 0
      ? 'Please fix the highlighted fields.'
      : error instanceof Error
        ? error.message
        : String(error)

  return (
    <div className="error-banner" role="alert">
      {message}
    </div>
  )
}

export function Loading() {
  return <p className="muted">Loading…</p>
}

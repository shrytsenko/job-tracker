import type { ApplicationStatus } from '../api/types'
import { label, statusTone } from '../labels'

export function StatusBadge({ status }: { status: ApplicationStatus }) {
  return <span className={`badge badge-${statusTone(status)}`}>{label(status)}</span>
}

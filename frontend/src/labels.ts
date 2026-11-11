import type { ApplicationStatus } from './api/types'

/** Turns enum values into readable labels: "RecruiterScreen" → "Recruiter screen". */
const overrides: Record<string, string> = {
  SDET: 'SDET',
  LinkedIn: 'LinkedIn',
  CompanyWebsite: 'Company website',
  ExColleague: 'Ex-colleague',
  NoResponse: 'No response',
  TakeHome: 'Take-home',
}

export function label(value: string): string {
  if (overrides[value]) return overrides[value]
  const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  return spaced.charAt(0).toUpperCase() + spaced.slice(1)
}

/** Groups statuses for badge colours. */
export function statusTone(status: ApplicationStatus): 'neutral' | 'active' | 'good' | 'closed' {
  switch (status) {
    case 'Interested':
      return 'neutral'
    case 'Offer':
    case 'Accepted':
      return 'good'
    case 'Rejected':
    case 'Withdrawn':
    case 'NoResponse':
      return 'closed'
    default:
      return 'active'
  }
}

/** Formats an ISO date for display; empty dash when missing. */
export function formatDate(date: string | null | undefined): string {
  if (!date) return '—'
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

/** Today as "YYYY-MM-DD" in local time (what <input type="date"> uses). */
export function todayIso(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

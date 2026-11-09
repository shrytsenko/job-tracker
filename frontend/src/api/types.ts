// Mirrors the API contracts in backend/src/JobTracker.Api/Contracts.
// Enums travel as strings, so each one is a const array (for <select> options) plus a union type.

export const applicationStatuses = [
  'Interested',
  'Applied',
  'RecruiterScreen',
  'TechInterview',
  'FinalInterview',
  'Offer',
  'Accepted',
  'Rejected',
  'Withdrawn',
  'NoResponse',
] as const
export type ApplicationStatus = (typeof applicationStatuses)[number]

export const tracks = ['Developer', 'SDET', 'Integration', 'Other'] as const
export type Track = (typeof tracks)[number]

export const channels = ['LinkedIn', 'Djinni', 'Email', 'Referral', 'Platform', 'CompanyWebsite', 'Other'] as const
export type Channel = (typeof channels)[number]

export const employmentTypes = ['Permanent', 'Contract', 'Freelance'] as const
export type EmploymentType = (typeof employmentTypes)[number]

export const compensationPeriods = ['Hour', 'Day', 'Month', 'Year'] as const
export type CompensationPeriod = (typeof compensationPeriods)[number]

// Keep in sync with backend/src/JobTracker.Core/Enums/Currency.cs.
export const currencies = ['EUR', 'USD', 'GBP', 'UAH', 'PLN', 'CHF'] as const
export type Currency = (typeof currencies)[number]

export const activityTypes = ['MessageSent', 'ReplyReceived', 'Interview', 'Note'] as const
export type ActivityType = (typeof activityTypes)[number]

export const contactRoles = ['Recruiter', 'Referrer', 'ExColleague', 'HiringManager', 'Other'] as const
export type ContactRole = (typeof contactRoles)[number]

/** ISO date, "YYYY-MM-DD" (C# DateOnly). */
export type IsoDate = string

export interface ApplicationSummary {
  id: string
  company: string
  roleTitle: string
  track: Track
  channel: Channel
  status: ApplicationStatus
  employmentType: EmploymentType
  appliedOn: IsoDate | null
  lastContactOn: IsoDate | null
  nextActionDescription: string | null
  nextActionDueOn: IsoDate | null
}

export interface ApplicationDetail {
  id: string
  company: string
  roleTitle: string
  jobUrl: string | null
  track: Track
  channel: Channel
  status: ApplicationStatus
  employmentType: EmploymentType
  remoteRegion: string | null
  hybridCity: string | null
  compensationAmount: number | null
  compensationCurrency: Currency | null
  compensationPeriod: CompensationPeriod | null
  compensationIsNet: boolean | null
  appliedOn: IsoDate | null
  lastContactOn: IsoDate | null
  nextActionDescription: string | null
  nextActionDueOn: IsoDate | null
  notes: string | null
  contacts: ContactSummary[]
  activities: Activity[]
}

export interface SaveApplicationRequest {
  company: string
  roleTitle: string
  track: Track
  channel: Channel
  employmentType: EmploymentType
  status: ApplicationStatus | null
  jobUrl: string | null
  remoteRegion: string | null
  hybridCity: string | null
  compensationAmount: number | null
  compensationCurrency: Currency | null
  compensationPeriod: CompensationPeriod | null
  compensationIsNet: boolean | null
  appliedOn: IsoDate | null
  lastContactOn: IsoDate | null
  nextActionDescription: string | null
  nextActionDueOn: IsoDate | null
  notes: string | null
}

export interface ApplicationFilters {
  status?: ApplicationStatus
  track?: Track
  channel?: Channel
  search?: string
}

export interface Activity {
  id: string
  type: ActivityType
  text: string
  date: IsoDate
}

export interface AddActivityRequest {
  type: ActivityType
  text: string
  date: IsoDate | null
}

export interface SetNextActionRequest {
  description: string | null
  dueOn: IsoDate | null
}

export interface CompleteNextActionRequest {
  type: ActivityType
  date: IsoDate | null
}

export interface FollowUp {
  application: ApplicationSummary
  daysSinceContact: number | null
  isStale: boolean
  isNextActionDue: boolean
}

export interface ContactSummary {
  id: string
  name: string
  role: ContactRole
  company: string | null
}

export interface ContactDetail {
  id: string
  name: string
  role: ContactRole
  company: string | null
  linkedInUrl: string | null
  email: string | null
  notes: string | null
  lastContactOn: IsoDate | null
  applications: ApplicationSummary[]
}

export interface SaveContactRequest {
  name: string
  role: ContactRole
  company: string | null
  linkedInUrl: string | null
  email: string | null
  notes: string | null
  lastContactOn: IsoDate | null
}

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

/** The hiring pipeline in order (mirrors Application.PipelineStages in the backend). */
export const pipelineStages = ['Applied', 'RecruiterScreen', 'TechInterview', 'FinalInterview', 'Offer'] as const
export type PipelineStage = (typeof pipelineStages)[number]

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

export const activityTypes = ['MessageSent', 'ReplyReceived', 'Interview', 'TakeHome', 'Note'] as const
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
  /** Furthest pipeline stage reached; kept after the application closes. */
  furthestStage: PipelineStage | null
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
  cvVersion: string | null
  track: Track
  channel: Channel
  status: ApplicationStatus
  /** Furthest pipeline stage reached; kept after the application closes. */
  furthestStage: PipelineStage | null
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
  /** The pasted job post, kept so it can be re-analysed. */
  jobDescription: string | null
  /** Latest AI analysis of the job post; null until one succeeds. */
  analysis: JobAnalysis | null
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
  cvVersion: string | null
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

export interface FunnelStep {
  stage: ApplicationStatus
  /** Applications whose furthest stage is this stage or later. */
  reached: number
  /** Reached / reached at the previous step (0–1); null for the first step or after an empty step. */
  conversionFromPrevious: number | null
}

export interface TrackFunnel {
  track: Track
  /** All applications on the track, including ones not applied to yet. */
  total: number
  steps: FunnelStep[]
  /** Offers / applied (0–1); null when nothing was applied to. */
  overallConversion: number | null
}

export interface DashboardStats {
  total: number
  byStatus: { status: ApplicationStatus; count: number }[]
  byTrack: { track: Track; count: number }[]
  funnels: TrackFunnel[]
}

export interface JobAnalysis {
  stack: string[]
  seniority: string | null
  locationRestrictions: string | null
  employmentType: string | null
  salary: string | null
  /** 0–100: how well the CV fits the role. */
  matchScore: number
  strengths: string[]
  gaps: string[]
  redFlags: string[]
  model: string
  analyzedAt: string
}

export interface Profile {
  cvSummary: string
  /** Used to sign reply drafts. */
  fullName: string | null
  /** Null until the profile has been saved once. */
  updatedAt: string | null
}

export const replyChannels = ['LinkedIn', 'Email'] as const
export type ReplyChannel = (typeof replyChannels)[number]

export const replyTones = ['Short', 'Detailed'] as const
export type ReplyTone = (typeof replyTones)[number]

export interface DraftReplyRequest {
  channel: ReplyChannel
  tone: ReplyTone
  contactId: string | null
  theirMessage: string | null
  intent: string | null
}

export interface ReplyDraft {
  /** Email subject; null for LinkedIn. */
  subject: string | null
  body: string
  model: string
}

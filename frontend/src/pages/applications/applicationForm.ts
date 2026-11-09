import type {
  ApplicationDetail,
  ApplicationStatus,
  Channel,
  CompensationPeriod,
  Currency,
  EmploymentType,
  SaveApplicationRequest,
  Track,
} from '../../api/types'

/**
 * Form state: every input is a string ("" = empty), which is what HTML inputs work with.
 * Converting to and from the API request in one place keeps the form component simple.
 */
export interface ApplicationFormValues {
  company: string
  roleTitle: string
  jobUrl: string
  track: Track
  channel: Channel
  employmentType: EmploymentType
  status: ApplicationStatus
  remoteRegion: string
  hybridCity: string
  compensationAmount: string
  compensationCurrency: Currency | ''
  compensationPeriod: CompensationPeriod | ''
  /** "", "net" or "gross". */
  compensationBasis: '' | 'net' | 'gross'
  appliedOn: string
  lastContactOn: string
  nextActionDescription: string
  nextActionDueOn: string
  notes: string
}

export const emptyApplicationForm: ApplicationFormValues = {
  company: '',
  roleTitle: '',
  jobUrl: '',
  track: 'Developer',
  channel: 'LinkedIn',
  employmentType: 'Permanent',
  status: 'Interested',
  remoteRegion: '',
  hybridCity: '',
  compensationAmount: '',
  compensationCurrency: '',
  compensationPeriod: '',
  compensationBasis: '',
  appliedOn: '',
  lastContactOn: '',
  nextActionDescription: '',
  nextActionDueOn: '',
  notes: '',
}

export function formFromDetail(a: ApplicationDetail): ApplicationFormValues {
  return {
    company: a.company,
    roleTitle: a.roleTitle,
    jobUrl: a.jobUrl ?? '',
    track: a.track,
    channel: a.channel,
    employmentType: a.employmentType,
    status: a.status,
    remoteRegion: a.remoteRegion ?? '',
    hybridCity: a.hybridCity ?? '',
    compensationAmount: a.compensationAmount?.toString() ?? '',
    compensationCurrency: a.compensationCurrency ?? '',
    compensationPeriod: a.compensationPeriod ?? '',
    compensationBasis: a.compensationIsNet === null ? '' : a.compensationIsNet ? 'net' : 'gross',
    appliedOn: a.appliedOn ?? '',
    lastContactOn: a.lastContactOn ?? '',
    nextActionDescription: a.nextActionDescription ?? '',
    nextActionDueOn: a.nextActionDueOn ?? '',
    notes: a.notes ?? '',
  }
}

const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

export function requestFromForm(v: ApplicationFormValues): SaveApplicationRequest {
  return {
    company: v.company.trim(),
    roleTitle: v.roleTitle.trim(),
    jobUrl: orNull(v.jobUrl),
    track: v.track,
    channel: v.channel,
    employmentType: v.employmentType,
    status: v.status,
    remoteRegion: orNull(v.remoteRegion),
    hybridCity: orNull(v.hybridCity),
    compensationAmount: v.compensationAmount.trim() === '' ? null : Number(v.compensationAmount),
    compensationCurrency: v.compensationCurrency || null,
    compensationPeriod: v.compensationPeriod || null,
    compensationIsNet: v.compensationBasis === '' ? null : v.compensationBasis === 'net',
    appliedOn: v.appliedOn || null,
    lastContactOn: v.lastContactOn || null,
    nextActionDescription: orNull(v.nextActionDescription),
    nextActionDueOn: v.nextActionDueOn || null,
    notes: orNull(v.notes),
  }
}

/** The API's PUT replaces all editable fields, so a quick status change resends everything else unchanged. */
export function requestWithStatus(a: ApplicationDetail, status: ApplicationStatus): SaveApplicationRequest {
  return { ...requestFromForm(formFromDetail(a)), status }
}

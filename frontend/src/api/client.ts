import type {
  Activity,
  AddActivityRequest,
  ApplicationDetail,
  ApplicationFilters,
  ApplicationSummary,
  CompleteNextActionRequest,
  ContactDetail,
  ContactSummary,
  FollowUp,
  SaveApplicationRequest,
  SaveContactRequest,
  SetNextActionRequest,
} from './types'

/** Error carrying the API's ProblemDetails, including per-field validation messages. */
export class ApiError extends Error {
  readonly status: number
  /** Field name (camelCase, as used in the request) → messages. */
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers,
  })

  if (!response.ok) {
    throw await toApiError(response)
  }

  // 204 No Content (delete, link/unlink) has no body to parse.
  return (response.status === 204 ? undefined : await response.json()) as T
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem: ProblemDetails = await response.json().catch(() => ({}))
  const fieldErrors: Record<string, string[]> = {}

  // The API reports fields in PascalCase ("Company"); the UI uses camelCase ("company").
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    fieldErrors[key.charAt(0).toLowerCase() + key.slice(1)] = messages
  }

  const message = problem.detail ?? problem.title ?? `Request failed (${response.status})`
  return new ApiError(response.status, message, fieldErrors)
}

const json = (body: unknown) => JSON.stringify(body)

function toQuery(params: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') search.set(key, String(value))
  }
  const query = search.toString()
  return query ? `?${query}` : ''
}

export const api = {
  listApplications: (filters: ApplicationFilters = {}) =>
    request<ApplicationSummary[]>(`/api/applications${toQuery({ ...filters })}`),
  getApplication: (id: string) => request<ApplicationDetail>(`/api/applications/${id}`),
  createApplication: (body: SaveApplicationRequest) =>
    request<ApplicationDetail>('/api/applications', { method: 'POST', body: json(body) }),
  updateApplication: (id: string, body: SaveApplicationRequest) =>
    request<ApplicationDetail>(`/api/applications/${id}`, { method: 'PUT', body: json(body) }),
  deleteApplication: (id: string) => request<void>(`/api/applications/${id}`, { method: 'DELETE' }),

  addActivity: (applicationId: string, body: AddActivityRequest) =>
    request<Activity>(`/api/applications/${applicationId}/activities`, { method: 'POST', body: json(body) }),
  linkContact: (applicationId: string, contactId: string) =>
    request<void>(`/api/applications/${applicationId}/contacts/${contactId}`, { method: 'PUT' }),
  unlinkContact: (applicationId: string, contactId: string) =>
    request<void>(`/api/applications/${applicationId}/contacts/${contactId}`, { method: 'DELETE' }),

  setNextAction: (applicationId: string, body: SetNextActionRequest) =>
    request<ApplicationDetail>(`/api/applications/${applicationId}/next-action`, { method: 'PUT', body: json(body) }),
  completeNextAction: (applicationId: string, body: CompleteNextActionRequest) =>
    request<ApplicationDetail>(`/api/applications/${applicationId}/next-action/complete`, { method: 'POST', body: json(body) }),

  getFollowUps: (days: number) => request<FollowUp[]>(`/api/applications/follow-ups${toQuery({ days })}`),

  listCompanies: () => request<string[]>('/api/companies'),
  listCvVersions: () => request<string[]>('/api/applications/cv-versions'),

  listContacts: () => request<ContactSummary[]>('/api/contacts'),
  getContact: (id: string) => request<ContactDetail>(`/api/contacts/${id}`),
  createContact: (body: SaveContactRequest) =>
    request<ContactDetail>('/api/contacts', { method: 'POST', body: json(body) }),
  updateContact: (id: string, body: SaveContactRequest) =>
    request<ContactDetail>(`/api/contacts/${id}`, { method: 'PUT', body: json(body) }),
  deleteContact: (id: string) => request<void>(`/api/contacts/${id}`, { method: 'DELETE' }),
}

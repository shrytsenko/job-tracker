import { useId } from 'react'
import { api } from '../api/client'
import { useAsync } from '../hooks/useAsync'

interface CompanyInputProps {
  value: string
  onChange: (value: string) => void
  autoFocus?: boolean
}

/**
 * Free-text company input that suggests names already used on applications and contacts,
 * so "Contoso" doesn't drift into "Contoso Ltd". A native <datalist>: no library needed,
 * and typing a brand-new name still works.
 */
export function CompanyInput({ value, onChange, autoFocus }: CompanyInputProps) {
  const listId = useId()
  // Suggestions are a convenience: if loading them fails, the input still works as plain text.
  const { data: companies } = useAsync(() => api.listCompanies(), [])

  return (
    <>
      <input list={listId} value={value} onChange={(e) => onChange(e.target.value)} autoFocus={autoFocus} autoComplete="off" />
      <datalist id={listId}>
        {companies?.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
    </>
  )
}

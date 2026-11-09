import { useId } from 'react'
import { useAsync } from '../hooks/useAsync'

interface SuggestionInputProps {
  value: string
  onChange: (value: string) => void
  /** Loads the names to suggest, e.g. companies or CV versions already used. */
  loadSuggestions: () => Promise<string[]>
  placeholder?: string
  autoFocus?: boolean
}

/**
 * Free-text input that suggests values already in use, so "Contoso" doesn't drift into "Contoso Ltd".
 * A native <datalist>: no library needed, and typing a brand-new value still works.
 */
export function SuggestionInput({ value, onChange, loadSuggestions, placeholder, autoFocus }: SuggestionInputProps) {
  const listId = useId()
  // Suggestions are a convenience: if loading them fails, the input still works as plain text.
  // eslint-disable-next-line react-hooks/exhaustive-deps -- load once per mount
  const { data: suggestions } = useAsync(loadSuggestions, [])

  return (
    <>
      <input
        list={listId}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        autoFocus={autoFocus}
        autoComplete="off"
      />
      <datalist id={listId}>
        {suggestions?.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
    </>
  )
}

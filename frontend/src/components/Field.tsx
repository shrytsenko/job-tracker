import { useId } from 'react'
import type { ReactNode } from 'react'

interface FieldProps {
  label: string
  /** Short explanation of what the field is for, shown from an ⓘ next to the label. */
  help?: string
  /** Server-side validation messages for this field, if any. */
  errors?: string[]
  children: ReactNode
  wide?: boolean
}

/** A labelled form control with optional help and its validation messages underneath. */
export function Field({ label, help, errors, children, wide }: FieldProps) {
  return (
    <label className={`field${wide ? ' field-wide' : ''}${errors?.length ? ' field-invalid' : ''}`}>
      <span className="field-label">{help ? <HelpLabel text={label} help={help} /> : label}</span>
      {children}
      {errors?.map((message) => (
        <span key={message} className="field-error">
          {message}
        </span>
      ))}
    </label>
  )
}

/**
 * A label with an explanation: dotted underline, like an abbreviation. The explanation shows on hover and on
 * keyboard focus (the label text is focusable so keyboard and screen-reader users get it too). Escape hides it.
 * Only for fields with a rule or side effect you can't guess from the name.
 */
function HelpLabel({ text, help }: { text: string; help: string }) {
  const id = useId()

  return (
    <span className="field-help">
      <span
        className="field-help-trigger"
        tabIndex={0}
        aria-describedby={id}
        onKeyDown={(e) => {
          if (e.key === 'Escape') e.currentTarget.blur()
        }}
      >
        {text}
      </span>
      <span role="tooltip" id={id} className="field-help-text">
        {help}
      </span>
    </span>
  )
}

interface SelectProps<T extends string> {
  value: T | ''
  options: readonly T[]
  onChange: (value: T | '') => void
  /** Shown as the empty option; omit to make a value required. */
  placeholder?: string
  formatOption?: (value: T) => string
  name?: string
}

/** A <select> over one of the enum arrays from api/types. */
export function Select<T extends string>({
  value,
  options,
  onChange,
  placeholder,
  formatOption,
  name,
}: SelectProps<T>) {
  return (
    <select name={name} value={value} onChange={(e) => onChange(e.target.value as T | '')}>
      {placeholder !== undefined && <option value="">{placeholder}</option>}
      {options.map((option) => (
        <option key={option} value={option}>
          {formatOption ? formatOption(option) : option}
        </option>
      ))}
    </select>
  )
}

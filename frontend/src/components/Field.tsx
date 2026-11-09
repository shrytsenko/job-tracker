import type { ReactNode } from 'react'

interface FieldProps {
  label: string
  /** Server-side validation messages for this field, if any. */
  errors?: string[]
  children: ReactNode
  wide?: boolean
}

/** A labelled form control with its validation messages underneath. */
export function Field({ label, errors, children, wide }: FieldProps) {
  return (
    <label className={`field${wide ? ' field-wide' : ''}${errors?.length ? ' field-invalid' : ''}`}>
      <span className="field-label">{label}</span>
      {children}
      {errors?.map((message) => (
        <span key={message} className="field-error">
          {message}
        </span>
      ))}
    </label>
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
export function Select<T extends string>({ value, options, onChange, placeholder, formatOption, name }: SelectProps<T>) {
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

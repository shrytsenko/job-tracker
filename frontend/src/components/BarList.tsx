import type { CSSProperties } from 'react'

export interface BarRow {
  key: string
  label: string
  value: number
  /** Short secondary figure shown after the value, e.g. a conversion rate. */
  note?: string
  /** Full sentence for the hover/focus tooltip and screen readers. */
  description: string
}

interface BarListProps {
  rows: BarRow[]
  /** The value that fills the whole track. Pass the same max to several lists to put them on one scale. */
  max: number
  label: string
}

/**
 * Horizontal bars on one baseline: label, bar, value at the tip. One hue; the value and label use text colours.
 * Each row is focusable, and its tooltip shows on hover or keyboard focus.
 */
export function BarList({ rows, max, label }: BarListProps) {
  return (
    <ul className="bar-list" aria-label={label}>
      {rows.map((row) => {
        const share = max > 0 ? row.value / max : 0
        return (
          <li key={row.key} className="bar-row" tabIndex={0} aria-label={row.description}>
            <span className="bar-label">{row.label}</span>
            <span className="bar-track" aria-hidden="true">
              {row.value > 0 && <span className="bar-fill" style={{ '--share': share } as CSSProperties} />}
              <span className="bar-value">
                {row.value}
                {row.note && <span className="bar-note">{row.note}</span>}
              </span>
            </span>
            <span className="bar-tooltip" aria-hidden="true">
              {row.description}
            </span>
          </li>
        )
      })}
    </ul>
  )
}

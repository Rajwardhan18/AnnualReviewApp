interface Props {
  value: number
  onChange?: (v: number) => void
  readonly?: boolean
  max?: number
  showValue?: boolean
}

/** 1-10 star rating input (or read-only display) with a numeric readout. */
export default function StarRating({ value, onChange, readonly, max = 10, showValue = true }: Props) {
  return (
    <span className="rating-wrap">
      <span className={`stars${readonly ? ' readonly' : ''}`} role="radiogroup" aria-label="rating">
        {Array.from({ length: max }, (_, i) => i + 1).map((n) => (
          <span
            key={n}
            className={`star${n <= value ? ' on' : ''}`}
            role={readonly ? undefined : 'radio'}
            aria-checked={n === value}
            onClick={readonly ? undefined : () => onChange?.(n)}
            title={`${n} / ${max}`}
          >
            ★
          </span>
        ))}
      </span>
      {showValue && <span className="rating-num">{value || 0}/{max}</span>}
    </span>
  )
}

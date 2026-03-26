import CopyButton from './CopyButton'
import './CopyableId.css'

export default function CopyableId({ value, className = '' }) {
  return (
    <span className={`copyable-id ${className}`}>
      <span className="copyable-id-value">{value}</span>
      <CopyButton className="copyable-id-btn" value={value} size={14} />
    </span>
  )
}

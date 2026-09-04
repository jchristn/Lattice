import Modal from './Modal'
import CopyableId from './CopyableId'
import './DetailModal.css'

// A read-only, formatted detail view. Each field is `{ label, value, title?, copyable?, multiline?, rows? }`.
// Falsy field entries are skipped so callers can conditionally include fields inline.
export default function DetailModal({ isOpen, onClose, title, subtitle, fields = [] }) {
  const visible = fields.filter(Boolean)

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} wide>
      <div className="detail-modal">
        {subtitle ? <p className="detail-modal-subtitle">{subtitle}</p> : null}
        <div className="detail-grid">
          {visible.map((field, index) => {
            const value = field.value === null || field.value === undefined ? '' : String(field.value)
            return (
              <div className={`detail-field ${field.multiline ? 'detail-field-full' : ''}`} key={index}>
                <label className="detail-label" title={field.title || undefined}>
                  {field.label}
                </label>
                {field.copyable && value ? (
                  <div className="detail-value-copyable" title={field.title || undefined}>
                    <CopyableId value={value} />
                  </div>
                ) : field.multiline ? (
                  <textarea
                    className="detail-value detail-textarea"
                    readOnly
                    value={value}
                    rows={field.rows || 5}
                    spellCheck={false}
                    title={field.title || 'Read-only value'}
                  />
                ) : (
                  <input
                    className="detail-value"
                    readOnly
                    value={value}
                    title={field.title || 'Read-only value'}
                  />
                )}
              </div>
            )
          })}
        </div>
      </div>
    </Modal>
  )
}

import Modal from './Modal'
import CopyableId from './CopyableId'
import './DetailModal.css'

// A read-only, formatted detail view rendered as a responsive grid of labeled value cards. Each field is
// `{ label, value, title?, copyable?, full?, code?, node? }`:
//   copyable -> a CopyableId chip
//   code     -> a wrapping/scrolling monospace block (for JSON or long text); spans the full width
//   node     -> arbitrary formatted JSX as the value (e.g. a structured list); spans the full width
//   full     -> spans the full width even when not code/node
// Falsy field entries are skipped so callers can inline conditionals. Values always wrap, so nothing is
// clipped, and the modal is extra-wide to give multi-field records room to breathe.
export default function DetailModal({ isOpen, onClose, title, subtitle, fields = [] }) {
  const visible = fields.filter(Boolean)

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} subtitle={subtitle} extraWide>
      <div className="detail-modal">
        <div className="detail-grid">
          {visible.map((field, index) => {
            const raw = field.value === null || field.value === undefined ? '' : field.value
            const value = String(raw)
            const isCode = !!field.code || !!field.multiline
            const spanFull = isCode || !!field.full || !!field.node

            return (
              <div
                className={`detail-item ${spanFull ? 'detail-item-full' : ''}`}
                key={index}
                title={field.title || undefined}
              >
                <span className="detail-item-label">{field.label}</span>
                {field.node ? (
                  <div className="detail-item-node">{field.node}</div>
                ) : field.copyable && value ? (
                  <div className="detail-item-value detail-item-copyable">
                    <CopyableId value={value} />
                  </div>
                ) : isCode ? (
                  <pre className="detail-item-code">{value || '—'}</pre>
                ) : (
                  <span className="detail-item-value">{value || '—'}</span>
                )}
              </div>
            )
          })}
        </div>
      </div>
    </Modal>
  )
}

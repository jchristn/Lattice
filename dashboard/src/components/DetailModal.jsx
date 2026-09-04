import { Fragment } from 'react'
import Modal from './Modal'
import CopyableId from './CopyableId'
import './DetailModal.css'

// A read-only detail view laid out as a clean, borderless key/value table (label on the left, value on
// the right) — no cards. IDs (copyable, monospace) naturally sort to the top and timestamps to the bottom;
// large/structured content (a `node`, or `code`/`multiline` text) renders in collapsible sections between
// the main rows and the timestamps.
//
// Each field is `{ label, value, title?, copyable?, mono?, code?, node?, timestamp?, defaultOpen? }`.
// Falsy field entries are skipped so callers can inline conditionals.

const TIMESTAMP_LABEL = /^(created|updated|last\s|modified)/i

function isSection(field) {
  return !!field.node || !!field.code || !!field.multiline
}

function isTimestamp(field) {
  return field.timestamp === true || (!isSection(field) && TIMESTAMP_LABEL.test(field.label || ''))
}

function KvRows({ fields }) {
  return (
    <dl className="detail-kv">
      {fields.map((field, index) => {
        const value = field.value === null || field.value === undefined ? '' : String(field.value)
        let rendered
        if (field.copyable && value) rendered = <CopyableId value={value} />
        else if (field.mono) rendered = <span className="detail-mono">{value || '—'}</span>
        else rendered = value === '' ? <span className="detail-empty">—</span> : value

        return (
          <Fragment key={index}>
            <dt title={field.title || undefined}>{field.label}</dt>
            <dd>{rendered}</dd>
          </Fragment>
        )
      })}
    </dl>
  )
}

export default function DetailModal({ isOpen, onClose, title, subtitle, fields = [] }) {
  const visible = fields.filter(Boolean)
  const sections = visible.filter(isSection)
  const rest = visible.filter((f) => !isSection(f))
  const timestamps = rest.filter(isTimestamp)
  const mainRows = rest.filter((f) => !isTimestamp(f))

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} subtitle={subtitle} wide>
      <div className="detail-modal">
        {mainRows.length > 0 ? <KvRows fields={mainRows} /> : null}

        {sections.map((field, index) => (
          <details className="detail-section" key={index} open={field.defaultOpen !== false}>
            <summary title={field.title || undefined}>{field.label}</summary>
            <div className="detail-section-body">
              {field.node ? field.node : <pre className="detail-code">{field.value ? String(field.value) : '—'}</pre>}
            </div>
          </details>
        ))}

        {timestamps.length > 0 ? (
          <div className="detail-timestamps">
            <KvRows fields={timestamps} />
          </div>
        ) : null}
      </div>
    </Modal>
  )
}

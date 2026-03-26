import Modal from './Modal'
import CopyButton from './CopyButton'
import CopyableId from './CopyableId'
import './JsonViewerModal.css'

export default function JsonViewerModal({ isOpen, onClose, title, subtitle, identifier, value }) {
  const jsonString = typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} wide>
      <div className="json-viewer-modal">
        {subtitle ? <p className="json-viewer-subtitle">{subtitle}</p> : null}
        {identifier ? (
          <div className="json-viewer-identifier">
            <CopyableId value={identifier} />
          </div>
        ) : null}
        <div className="json-viewer-content">
          <div className="json-viewer-toolbar">
            <span className="json-viewer-label">JSON</span>
            <CopyButton value={jsonString} />
          </div>
          <pre className="json-viewer-pre">{jsonString}</pre>
        </div>
      </div>
    </Modal>
  )
}

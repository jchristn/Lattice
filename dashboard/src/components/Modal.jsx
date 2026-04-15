import './Modal.css'

export default function Modal({ isOpen, onClose, title, subtitle, children, wide, extraWide }) {
  if (!isOpen) return null

  const sizeClass = extraWide ? 'modal-extra-wide' : wide ? 'modal-wide' : ''

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className={`modal-content ${sizeClass}`} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-title-wrap">
            <h2 className="modal-title">{title}</h2>
            {subtitle ? <p className="modal-subtitle">{subtitle}</p> : null}
          </div>
          <button className="modal-close" onClick={onClose}>
            &times;
          </button>
        </div>
        <div className="modal-body">{children}</div>
      </div>
    </div>
  )
}

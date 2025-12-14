import { useState, useRef, useEffect } from 'react'
import './ActionMenu.css'

export default function ActionMenu({ items }) {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef(null)

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleItemClick = (item) => {
    setIsOpen(false)
    item.onClick()
  }

  return (
    <div className="action-menu" ref={menuRef}>
      <button
        className="action-menu-trigger"
        onClick={() => setIsOpen(!isOpen)}
        aria-label="Actions"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
          <circle cx="12" cy="5" r="2"></circle>
          <circle cx="12" cy="12" r="2"></circle>
          <circle cx="12" cy="19" r="2"></circle>
        </svg>
      </button>
      {isOpen && (
        <div className="action-menu-dropdown">
          {items.map((item, index) => (
            <button
              key={index}
              className={`action-menu-item ${item.variant === 'danger' ? 'danger' : ''}`}
              onClick={() => handleItemClick(item)}
            >
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

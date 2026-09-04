import { useState, useRef, useEffect, useLayoutEffect, useCallback } from 'react'
import { createPortal } from 'react-dom'
import './ActionMenu.css'

// Viewport margin kept between the menu and the window edges.
const MARGIN = 8

export default function ActionMenu({ items }) {
  const [isOpen, setIsOpen] = useState(false)
  const [coords, setCoords] = useState({ top: 0, left: 0 })
  const triggerRef = useRef(null)
  const dropdownRef = useRef(null)

  // Position the dropdown relative to the trigger, clamped so it is always fully inside the viewport.
  // The menu is rendered in a portal with fixed positioning so it floats over the table instead of being
  // clipped by the table/card's overflow.
  const reposition = useCallback(() => {
    const trigger = triggerRef.current
    if (!trigger) return

    const rect = trigger.getBoundingClientRect()
    const menu = dropdownRef.current
    const menuWidth = menu ? menu.offsetWidth : 180
    const menuHeight = menu ? menu.offsetHeight : 0
    const vw = window.innerWidth
    const vh = window.innerHeight

    // Right-align to the trigger, then clamp horizontally into the viewport.
    let left = rect.right - menuWidth
    if (left + menuWidth > vw - MARGIN) left = vw - MARGIN - menuWidth
    if (left < MARGIN) left = MARGIN

    // Prefer below the trigger; flip above when there is not enough room, then clamp vertically.
    let top = rect.bottom + 4
    if (menuHeight && top + menuHeight > vh - MARGIN) {
      const above = rect.top - 4 - menuHeight
      top = above >= MARGIN ? above : Math.max(MARGIN, vh - MARGIN - menuHeight)
    }

    setCoords({ top, left })
  }, [])

  useLayoutEffect(() => {
    if (!isOpen) return undefined
    reposition()
    // Re-measure once the menu has its real dimensions.
    const raf = requestAnimationFrame(reposition)
    return () => cancelAnimationFrame(raf)
  }, [isOpen, reposition])

  useEffect(() => {
    if (!isOpen) return undefined

    const onReposition = () => reposition()
    const onClickOutside = (event) => {
      if (triggerRef.current && triggerRef.current.contains(event.target)) return
      if (dropdownRef.current && dropdownRef.current.contains(event.target)) return
      setIsOpen(false)
    }

    // Capture-phase scroll catches scrolling of any ancestor container, not just the window.
    window.addEventListener('scroll', onReposition, true)
    window.addEventListener('resize', onReposition)
    document.addEventListener('mousedown', onClickOutside)
    return () => {
      window.removeEventListener('scroll', onReposition, true)
      window.removeEventListener('resize', onReposition)
      document.removeEventListener('mousedown', onClickOutside)
    }
  }, [isOpen, reposition])

  const handleItemClick = (item) => {
    setIsOpen(false)
    item.onClick()
  }

  return (
    <div className="action-menu">
      <button
        ref={triggerRef}
        className="action-menu-trigger"
        onClick={(event) => {
          event.stopPropagation()
          setIsOpen((open) => !open)
        }}
        aria-label="Actions"
        title="Open the list of actions available for this row"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
          <circle cx="12" cy="5" r="2"></circle>
          <circle cx="12" cy="12" r="2"></circle>
          <circle cx="12" cy="19" r="2"></circle>
        </svg>
      </button>
      {isOpen &&
        createPortal(
          <div
            ref={dropdownRef}
            className="action-menu-dropdown"
            style={{ top: coords.top, left: coords.left }}
            onClick={(event) => event.stopPropagation()}
          >
            {items.map((item, index) => (
              <button
                key={index}
                className={`action-menu-item ${item.variant === 'danger' ? 'danger' : ''}`}
                onClick={() => handleItemClick(item)}
                title={item.title || undefined}
              >
                {item.label}
              </button>
            ))}
          </div>,
          document.body
        )}
    </div>
  )
}

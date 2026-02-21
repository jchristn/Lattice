import { useState, useEffect, useCallback } from 'react'
import './TourOverlay.css'

const TOUR_STEPS = [
  {
    selector: '.topbar',
    title: 'Top Bar',
    description: 'The top bar shows the Lattice logo, your connected server URL, a theme toggle for light/dark mode, and a disconnect button.',
    position: 'bottom',
  },
  {
    selector: '.sidebar-link[href="/collections"]',
    title: 'Collections',
    description: 'Collections are the primary containers for organizing your JSON documents. Think of them like tables in a database.',
    position: 'right',
  },
  {
    selector: '.sidebar-link[href="/documents"]',
    title: 'Documents',
    description: 'Browse and manage JSON documents stored within your collections. You can view, create, and delete documents here.',
    position: 'right',
  },
  {
    selector: '.sidebar-link[href="/schemas"]',
    title: 'Schemas',
    description: 'Lattice automatically detects schemas from your ingested documents. View the structure and types discovered across your data.',
    position: 'right',
  },
  {
    selector: '.sidebar-link[href="/schema-elements"]',
    title: 'Schema Elements',
    description: 'Inspect individual fields detected in your schemas, including their types, paths, and how they appear across documents.',
    position: 'right',
  },
  {
    selector: '.sidebar-link[href="/tables"]',
    title: 'Index Tables',
    description: 'View the internal index tables that Lattice builds for fast queries. These are automatically maintained as you add documents.',
    position: 'right',
  },
  {
    selector: '.sidebar-link[href="/entries"]',
    title: 'Index Entries',
    description: 'Browse individual index entries within the index tables. Useful for understanding how your data is indexed.',
    position: 'right',
  },
  {
    selector: '.sidebar-link[href="/search"]',
    title: 'Search',
    description: 'Query your documents using SQL-like expressions. Filter by labels, tags, and field values across your collections.',
    position: 'right',
  },
]

export default function TourOverlay({ isOpen, onComplete }) {
  const [step, setStep] = useState(-1) // -1 = welcome
  const [spotlightRect, setSpotlightRect] = useState(null)

  const updateSpotlight = useCallback(() => {
    if (step < 0 || step >= TOUR_STEPS.length) {
      setSpotlightRect(null)
      return
    }
    const el = document.querySelector(TOUR_STEPS[step].selector)
    if (el) {
      const rect = el.getBoundingClientRect()
      setSpotlightRect({
        top: rect.top - 4,
        left: rect.left - 4,
        width: rect.width + 8,
        height: rect.height + 8,
      })
    } else {
      setSpotlightRect(null)
    }
  }, [step])

  useEffect(() => {
    if (!isOpen) {
      setStep(-1)
      return
    }
    updateSpotlight()
    window.addEventListener('resize', updateSpotlight)
    return () => window.removeEventListener('resize', updateSpotlight)
  }, [isOpen, step, updateSpotlight])

  if (!isOpen) return null

  const handleNext = () => {
    if (step < TOUR_STEPS.length - 1) {
      setStep(step + 1)
    } else {
      onComplete()
    }
  }

  const handlePrev = () => {
    if (step > 0) {
      setStep(step - 1)
    } else {
      setStep(-1)
    }
  }

  const handleSkip = () => {
    onComplete()
  }

  const handleStart = () => {
    setStep(0)
  }

  // Welcome modal
  if (step === -1) {
    return (
      <div className="tour-welcome-overlay">
        <div className="tour-welcome-modal">
          <h1 className="tour-welcome-title">Welcome to Lattice</h1>
          <p className="tour-welcome-subtitle">
            Lattice is a powerful JSON document store with a rich dashboard for managing your data.
          </p>
          <div className="tour-welcome-features">
            <div className="tour-welcome-feature">
              <span className="tour-welcome-feature-icon">&#128270;</span>
              <div className="tour-welcome-feature-text">
                <strong>Schema Detection</strong>
                Automatically detects structure from your JSON documents
              </div>
            </div>
            <div className="tour-welcome-feature">
              <span className="tour-welcome-feature-icon">&#128269;</span>
              <div className="tour-welcome-feature-text">
                <strong>SQL-like Queries</strong>
                Search and filter documents with expressive queries
              </div>
            </div>
            <div className="tour-welcome-feature">
              <span className="tour-welcome-feature-icon">&#128451;</span>
              <div className="tour-welcome-feature-text">
                <strong>Multi-Database</strong>
                Supports multiple storage backends
              </div>
            </div>
            <div className="tour-welcome-feature">
              <span className="tour-welcome-feature-icon">&#128279;</span>
              <div className="tour-welcome-feature-text">
                <strong>REST API</strong>
                Full-featured API for programmatic access
              </div>
            </div>
          </div>
          <div className="tour-welcome-actions">
            <button className="tour-btn" onClick={handleSkip}>
              Skip Tour
            </button>
            <button className="tour-btn tour-btn-primary" onClick={handleStart}>
              Take the Tour
            </button>
          </div>
        </div>
      </div>
    )
  }

  // Highlight steps
  const currentStep = TOUR_STEPS[step]
  const tooltipStyle = {}
  if (spotlightRect) {
    if (currentStep.position === 'bottom') {
      tooltipStyle.top = spotlightRect.top + spotlightRect.height + 12
      tooltipStyle.left = spotlightRect.left
    } else if (currentStep.position === 'right') {
      tooltipStyle.top = spotlightRect.top
      tooltipStyle.left = spotlightRect.left + spotlightRect.width + 12
    }
  }

  return (
    <div className="tour-overlay">
      {spotlightRect && (
        <>
          <div className="tour-spotlight" style={spotlightRect} />
          <div className="tour-backdrop" onClick={handleSkip} />
        </>
      )}
      <div className="tour-tooltip" style={tooltipStyle}>
        <h3 className="tour-tooltip-title">{currentStep.title}</h3>
        <p className="tour-tooltip-description">{currentStep.description}</p>
        <div className="tour-tooltip-footer">
          <span className="tour-step-indicator">
            {step + 1} of {TOUR_STEPS.length}
          </span>
          <div className="tour-tooltip-actions">
            <button className="tour-btn-skip" onClick={handleSkip}>
              Skip
            </button>
            {step > 0 && (
              <button className="tour-btn" onClick={handlePrev}>
                Previous
              </button>
            )}
            <button className="tour-btn tour-btn-primary" onClick={handleNext}>
              {step === TOUR_STEPS.length - 1 ? 'Finish' : 'Next'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import Modal from './Modal'
import './SetupWizard.css'

const TOTAL_STEPS = 6

const SCHEMA_MODES = [
  { value: 'none', label: 'None', description: 'No schema enforcement. Documents can have any structure.' },
  { value: 'flexible', label: 'Flexible', description: 'Schema is detected but not enforced. Documents can deviate from the schema.' },
  { value: 'partial', label: 'Partial', description: 'Required fields are enforced, but additional fields are allowed.' },
  { value: 'strict', label: 'Strict', description: 'Documents must exactly match the detected schema.' },
]

const INDEX_MODES = [
  { value: 'all', label: 'All Fields', description: 'Index every field for maximum query flexibility. Uses more storage.' },
  { value: 'selective', label: 'Selective', description: 'Only index specific fields you choose. Balanced approach.' },
  { value: 'none', label: 'None', description: 'No indexing. Documents can still be retrieved but queries will be slower.' },
]

const DEFAULT_DOC = JSON.stringify({
  name: "Example Item",
  description: "My first document in Lattice",
  status: "active",
  tags: ["sample", "getting-started"],
  metadata: {
    version: 1,
    priority: "normal"
  }
}, null, 2)

export default function SetupWizard({ isOpen, onComplete }) {
  const { api } = useApp()
  const navigate = useNavigate()

  const [step, setStep] = useState(0)
  const [collectionName, setCollectionName] = useState('')
  const [collectionDescription, setCollectionDescription] = useState('')
  const [schemaMode, setSchemaMode] = useState('none')
  const [indexMode, setIndexMode] = useState('all')
  const [docJson, setDocJson] = useState(DEFAULT_DOC)
  const [docName, setDocName] = useState('')
  const [jsonError, setJsonError] = useState('')
  const [apiError, setApiError] = useState('')
  const [loading, setLoading] = useState(false)
  const [createdCollection, setCreatedCollection] = useState(null)

  if (!isOpen) return null

  const resetState = () => {
    setStep(0)
    setCollectionName('')
    setCollectionDescription('')
    setSchemaMode('none')
    setIndexMode('all')
    setDocJson(DEFAULT_DOC)
    setDocName('')
    setJsonError('')
    setApiError('')
    setLoading(false)
    setCreatedCollection(null)
  }

  const handleClose = () => {
    resetState()
    onComplete()
  }

  const validateJson = (value) => {
    try {
      JSON.parse(value)
      setJsonError('')
      return true
    } catch (e) {
      setJsonError(e.message)
      return false
    }
  }

  const handleCreateAndFinish = async () => {
    if (!api) {
      setApiError('Not connected to a server.')
      return
    }

    setLoading(true)
    setApiError('')

    try {
      // Create collection
      const collectionData = {
        name: collectionName,
        description: collectionDescription || undefined,
      }
      const collection = await api.createCollection(collectionData)
      const collectionId = collection?.GUID || collection?.guid || collection?.id

      if (!collectionId) {
        throw new Error('Collection was created but no ID was returned.')
      }

      setCreatedCollection({ id: collectionId, name: collectionName })

      // Create document if JSON is provided
      if (docJson.trim()) {
        if (!validateJson(docJson)) {
          setLoading(false)
          return
        }
        const parsedDoc = JSON.parse(docJson)
        const documentData = {
          content: parsedDoc,
        }
        if (docName.trim()) {
          documentData.name = docName.trim()
        }
        await api.createDocument(collectionId, documentData)
      }

      setStep(5) // Complete step
    } catch (err) {
      setApiError(err.message || 'An error occurred while creating resources.')
    } finally {
      setLoading(false)
    }
  }

  const handleNext = () => {
    if (step === 4) {
      // Validate JSON before proceeding
      if (docJson.trim() && !validateJson(docJson)) return
      handleCreateAndFinish()
      return
    }
    if (step === 1 && !collectionName.trim()) return
    setStep(step + 1)
    setApiError('')
  }

  const handleBack = () => {
    if (step > 0) {
      setStep(step - 1)
      setApiError('')
    }
  }

  const handleNavigate = (path) => {
    handleClose()
    navigate(path)
  }

  const renderStepper = () => (
    <div className="wizard-stepper">
      {Array.from({ length: TOTAL_STEPS }, (_, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
          <div className={`wizard-step-dot ${i === step ? 'active' : i < step ? 'completed' : ''}`} />
          {i < TOTAL_STEPS - 1 && (
            <div className={`wizard-step-connector ${i < step ? 'completed' : ''}`} />
          )}
        </div>
      ))}
    </div>
  )

  const renderStepContent = () => {
    switch (step) {
      case 0:
        return (
          <div className="wizard-content">
            <h3 className="wizard-step-title">Let's set up your first collection</h3>
            <p className="wizard-step-description">
              This wizard will walk you through creating your first collection and adding a document.
              Collections are containers that organize your JSON documents, similar to tables in a database.
            </p>
            <p className="wizard-step-description">
              We'll go through a few quick steps: naming your collection, configuring optional schema and indexing settings, and adding your first document.
            </p>
          </div>
        )

      case 1:
        return (
          <div className="wizard-content">
            <h3 className="wizard-step-title">Create a Collection</h3>
            <p className="wizard-step-description">
              Give your collection a name and optional description. Collections hold related JSON documents together.
            </p>
            <div className="wizard-form-group">
              <label>Collection Name *</label>
              <input
                type="text"
                value={collectionName}
                onChange={(e) => setCollectionName(e.target.value)}
                placeholder="e.g., products, users, events"
                autoFocus
              />
            </div>
            <div className="wizard-form-group">
              <label>Description (optional)</label>
              <textarea
                value={collectionDescription}
                onChange={(e) => setCollectionDescription(e.target.value)}
                placeholder="What kind of documents will this collection hold?"
              />
            </div>
          </div>
        )

      case 2:
        return (
          <div className="wizard-content">
            <h3 className="wizard-step-title">Schema Enforcement</h3>
            <p className="wizard-step-description">
              Choose how strictly Lattice should enforce document schemas. You can change this later.
            </p>
            <div className="wizard-radio-group">
              {SCHEMA_MODES.map(mode => (
                <label
                  key={mode.value}
                  className={`wizard-radio-option ${schemaMode === mode.value ? 'selected' : ''}`}
                >
                  <input
                    type="radio"
                    name="schemaMode"
                    value={mode.value}
                    checked={schemaMode === mode.value}
                    onChange={() => setSchemaMode(mode.value)}
                  />
                  <div>
                    <div className="wizard-radio-label">{mode.label}</div>
                    <div className="wizard-radio-description">{mode.description}</div>
                  </div>
                </label>
              ))}
            </div>
          </div>
        )

      case 3:
        return (
          <div className="wizard-content">
            <h3 className="wizard-step-title">Indexing Configuration</h3>
            <p className="wizard-step-description">
              Indexing enables fast queries on your documents. Choose how fields should be indexed.
            </p>
            <div className="wizard-radio-group">
              {INDEX_MODES.map(mode => (
                <label
                  key={mode.value}
                  className={`wizard-radio-option ${indexMode === mode.value ? 'selected' : ''}`}
                >
                  <input
                    type="radio"
                    name="indexMode"
                    value={mode.value}
                    checked={indexMode === mode.value}
                    onChange={() => setIndexMode(mode.value)}
                  />
                  <div>
                    <div className="wizard-radio-label">{mode.label}</div>
                    <div className="wizard-radio-description">{mode.description}</div>
                  </div>
                </label>
              ))}
            </div>
          </div>
        )

      case 4:
        return (
          <div className="wizard-content">
            <h3 className="wizard-step-title">Add Your First Document</h3>
            <p className="wizard-step-description">
              Documents are the JSON data stored in your collection. Edit the sample below or paste your own JSON.
            </p>
            <div className="wizard-form-group">
              <label>Document Name (optional)</label>
              <input
                type="text"
                value={docName}
                onChange={(e) => setDocName(e.target.value)}
                placeholder="e.g., my-first-document"
              />
            </div>
            <div className="wizard-form-group">
              <label>Document JSON</label>
              <textarea
                className="wizard-json-editor"
                value={docJson}
                onChange={(e) => {
                  setDocJson(e.target.value)
                  if (jsonError) validateJson(e.target.value)
                }}
              />
              {jsonError && <div className="wizard-json-error">Invalid JSON: {jsonError}</div>}
            </div>
            {apiError && <div className="wizard-error">{apiError}</div>}
          </div>
        )

      case 5:
        return (
          <div className="wizard-content">
            <div className="wizard-complete">
              <div className="wizard-complete-icon">&#9989;</div>
              <h3 className="wizard-complete-title">You're all set!</h3>
              <p className="wizard-complete-description">
                Your collection and document have been created successfully.
              </p>
              <div className="wizard-complete-summary">
                <div className="wizard-complete-summary-item">
                  <span className="wizard-complete-summary-label">Collection</span>
                  <span className="wizard-complete-summary-value">{collectionName}</span>
                </div>
                {docName && (
                  <div className="wizard-complete-summary-item">
                    <span className="wizard-complete-summary-label">Document</span>
                    <span className="wizard-complete-summary-value">{docName}</span>
                  </div>
                )}
                <div className="wizard-complete-summary-item">
                  <span className="wizard-complete-summary-label">Schema Mode</span>
                  <span className="wizard-complete-summary-value">{SCHEMA_MODES.find(m => m.value === schemaMode)?.label}</span>
                </div>
                <div className="wizard-complete-summary-item">
                  <span className="wizard-complete-summary-label">Indexing</span>
                  <span className="wizard-complete-summary-value">{INDEX_MODES.find(m => m.value === indexMode)?.label}</span>
                </div>
              </div>
              <div className="wizard-complete-links">
                <button className="wizard-btn" onClick={() => handleNavigate('/collections')}>
                  View Collections
                </button>
                {createdCollection && (
                  <button className="wizard-btn" onClick={() => handleNavigate(`/collections/${createdCollection.id}/documents`)}>
                    View Documents
                  </button>
                )}
                <button className="wizard-btn tour-btn-primary" onClick={() => handleNavigate('/search')}>
                  Try Search
                </button>
              </div>
            </div>
          </div>
        )

      default:
        return null
    }
  }

  const stepTitles = ['Welcome', 'Collection', 'Schema', 'Indexing', 'Document', 'Complete']

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={`Setup Wizard - ${stepTitles[step]}`} wide>
      {renderStepper()}
      <div style={{ padding: '20px 24px' }}>
        {renderStepContent()}
      </div>
      {step < 5 && (
        <div className="wizard-footer">
          <div className="wizard-footer-left">
            <button className="wizard-btn-skip" onClick={handleClose}>
              Skip Wizard
            </button>
          </div>
          <div className="wizard-footer-right">
            {step > 0 && (
              <button className="wizard-btn" onClick={handleBack} disabled={loading}>
                Back
              </button>
            )}
            {(step === 2 || step === 3) && (
              <button className="wizard-btn" onClick={() => { setStep(step + 1); setApiError('') }}>
                Skip
              </button>
            )}
            <button
              className="wizard-btn wizard-btn-primary"
              onClick={handleNext}
              disabled={loading || (step === 1 && !collectionName.trim())}
            >
              {loading ? 'Creating...' : step === 4 ? 'Create & Finish' : 'Next'}
            </button>
          </div>
        </div>
      )}
      {step === 5 && (
        <div className="wizard-footer">
          <div />
          <button className="wizard-btn wizard-btn-primary" onClick={handleClose}>
            Done
          </button>
        </div>
      )}
    </Modal>
  )
}

import { useEffect, useMemo, useRef, useState } from 'react'
import { useApp } from '../context/AppContext'
import CopyButton from '../components/CopyButton'
import CopyableId from '../components/CopyableId'
import KeyValueEditor from '../components/KeyValueEditor'
import { buildCodeSnippets, buildRequestPath, flattenOpenApiSpec, getParameterDefault, getRequestBodyTemplate } from '../utils/openApi'
import { formatDate } from '../utils/api'
import './ApiExplorer.css'

const STORAGE_KEY = 'lattice_api_explorer_state'
const HISTORY_KEY = 'lattice_api_explorer_history'
const MAX_HISTORY_ITEMS = 12

function loadState() {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) {
      return JSON.parse(saved)
    }
  } catch {
    // Ignore localStorage failures.
  }

  return {
    selectedOperationKey: '',
  }
}

function loadHistory() {
  try {
    const saved = localStorage.getItem(HISTORY_KEY)
    if (saved) {
      return JSON.parse(saved)
    }
  } catch {
    // Ignore localStorage failures.
  }

  return []
}

function buildDefaultParameterValues(spec, operation) {
  return operation.parameters.reduce((result, parameter) => {
    result[parameter.name] = getParameterDefault(spec, parameter)
    return result
  }, {})
}

function formatBytes(value) {
  if (!value) return '0 B'
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / (1024 * 1024)).toFixed(1)} MB`
}

function formatDuration(value) {
  if (!value && value !== 0) return '-'
  return `${value.toFixed(1)} ms`
}

function prettyPrint(value) {
  if (value === null || value === undefined || value === '') return '(empty)'
  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }
  return JSON.stringify(value, null, 2)
}

function getResponsePreview(response) {
  if (!response) return '(no response yet)'
  if (response.json && typeof response.json === 'object') {
    if ('success' in response.json) {
      return response.json.data ?? response.json
    }
    return response.json
  }
  return response.text || '(empty)'
}

function getResponseBodyText(response) {
  if (!response) return '(no response yet)'
  if (response.text) return response.text
  if (response.json !== null) return JSON.stringify(response.json, null, 2)
  return '(empty)'
}

export default function ApiExplorer() {
  const { api, serverUrl, setError } = useApp()
  const savedState = useMemo(() => loadState(), [])
  const [history, setHistory] = useState(() => loadHistory())
  const [spec, setSpec] = useState(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [sending, setSending] = useState(false)
  const [selectedOperationKey, setSelectedOperationKey] = useState(savedState.selectedOperationKey || '')
  const [parameterValues, setParameterValues] = useState({})
  const [extraQueryValues, setExtraQueryValues] = useState({})
  const [headerValues, setHeaderValues] = useState({})
  const [requestBody, setRequestBody] = useState('')
  const [responseTab, setResponseTab] = useState('preview')
  const [codeLanguage, setCodeLanguage] = useState('curl')
  const [response, setResponse] = useState(null)
  const [sendError, setSendError] = useState('')
  const restoreRef = useRef(false)
  const abortControllerRef = useRef(null)

  const operations = useMemo(() => flattenOpenApiSpec(spec || {}), [spec])
  const groupedOperations = useMemo(() => {
    return operations.reduce((result, operation) => {
      if (!result[operation.tag]) {
        result[operation.tag] = []
      }
      result[operation.tag].push(operation)
      return result
    }, {})
  }, [operations])

  const selectedOperation = useMemo(
    () => operations.find((operation) => operation.key === selectedOperationKey) || null,
    [operations, selectedOperationKey]
  )

  const pathParameters = selectedOperation?.parameters.filter((parameter) => parameter.in === 'path') || []
  const queryParameters = selectedOperation?.parameters.filter((parameter) => parameter.in === 'query') || []
  const requestPath = selectedOperation
    ? buildRequestPath(selectedOperation, parameterValues, extraQueryValues)
    : ''
  const codeSnippets = useMemo(() => {
    if (!selectedOperation || !requestPath) {
      return { curl: '', javascript: '', csharp: '' }
    }

    const effectiveHeaders = { ...headerValues }
    if (requestBody.trim() && selectedOperation.method !== 'GET' && selectedOperation.method !== 'HEAD') {
      effectiveHeaders['Content-Type'] = effectiveHeaders['Content-Type'] || 'application/json'
    }

    return buildCodeSnippets(
      serverUrl,
      selectedOperation.method,
      requestPath,
      effectiveHeaders,
      requestBody.trim() && selectedOperation.method !== 'GET' && selectedOperation.method !== 'HEAD' ? requestBody : ''
    )
  }, [headerValues, requestBody, requestPath, selectedOperation, serverUrl])

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ selectedOperationKey }))
  }, [selectedOperationKey])

  useEffect(() => {
    localStorage.setItem(HISTORY_KEY, JSON.stringify(history.slice(0, MAX_HISTORY_ITEMS)))
  }, [history])

  useEffect(() => {
    const loadSpec = async (showRefreshing = false) => {
      try {
        if (showRefreshing) setRefreshing(true)
        else setLoading(true)

        const data = await api.getOpenApiSpec()
        setSpec(data)
      } catch (err) {
        setError('Failed to load OpenAPI document: ' + err.message)
      } finally {
        setLoading(false)
        setRefreshing(false)
      }
    }

    loadSpec(false)
  }, [api, setError])

  useEffect(() => {
    if (!operations.length) return

    const operationExists = operations.some((operation) => operation.key === selectedOperationKey)
    if (!selectedOperationKey || !operationExists) {
      setSelectedOperationKey(operations[0].key)
    }
  }, [operations, selectedOperationKey])

  useEffect(() => {
    if (!selectedOperation || !spec) return
    if (restoreRef.current) {
      restoreRef.current = false
      return
    }

    setParameterValues(buildDefaultParameterValues(spec, selectedOperation))
    setExtraQueryValues({})
    setHeaderValues({})
    setRequestBody(getRequestBodyTemplate(spec, selectedOperation))
    setSendError('')
  }, [selectedOperation, spec])

  const refreshSpec = async () => {
    try {
      setRefreshing(true)
      const data = await api.getOpenApiSpec()
      setSpec(data)
    } catch (err) {
      setError('Failed to refresh OpenAPI document: ' + err.message)
    } finally {
      setRefreshing(false)
    }
  }

  const resetRequest = () => {
    if (!selectedOperation || !spec) return

    setParameterValues(buildDefaultParameterValues(spec, selectedOperation))
    setExtraQueryValues({})
    setHeaderValues({})
    setRequestBody(getRequestBodyTemplate(spec, selectedOperation))
    setSendError('')
  }

  const updateParameterValue = (name, value) => {
    setParameterValues((current) => ({
      ...current,
      [name]: value,
    }))
  }

  const sendRequest = async () => {
    if (!selectedOperation) return

    const missingPathParam = pathParameters.find((parameter) => parameter.required && !String(parameterValues[parameter.name] ?? '').trim())
    if (missingPathParam) {
      setSendError(`${missingPathParam.name} is required`)
      return
    }

    setSendError('')
    setSending(true)
    setResponse(null)
    setResponseTab('preview')

    const controller = new AbortController()
    abortControllerRef.current = controller

    try {
      const effectiveHeaders = Object.entries(headerValues).reduce((result, [key, value]) => {
        if (!key || value === null || value === undefined || value === '') return result
        result[key] = value
        return result
      }, {})
      const body = requestBody.trim() && selectedOperation.method !== 'GET' && selectedOperation.method !== 'HEAD'
        ? requestBody
        : null

      const rawResponse = await api.requestRaw(selectedOperation.method, requestPath, {
        body,
        headers: effectiveHeaders,
        signal: controller.signal,
      })

      setResponse(rawResponse)
      setHistory((current) => [
        {
          id: `${Date.now()}`,
          timestamp: new Date().toISOString(),
          operationKey: selectedOperation.key,
          label: `${selectedOperation.method} ${selectedOperation.path}`,
          requestPath,
          parameterValues,
          extraQueryValues,
          headerValues,
          requestBody,
          status: rawResponse.status,
          requestId: rawResponse.requestId,
        },
        ...current.filter((item) => !(item.operationKey === selectedOperation.key && item.requestPath === requestPath)).slice(0, MAX_HISTORY_ITEMS - 1),
      ])
    } catch (err) {
      if (err.name === 'AbortError') {
        setSendError('Request canceled')
        return
      }

      setSendError(err.message)
    } finally {
      abortControllerRef.current = null
      setSending(false)
    }
  }

  const loadHistoryItem = (item) => {
    restoreRef.current = true
    setSelectedOperationKey(item.operationKey)
    setParameterValues(item.parameterValues || {})
    setExtraQueryValues(item.extraQueryValues || {})
    setHeaderValues(item.headerValues || {})
    setRequestBody(item.requestBody || '')
    setSendError('')
  }

  const clearHistory = () => {
    if (!window.confirm('Clear API Explorer history?')) {
      return
    }

    setHistory([])
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="api-explorer">
      <div className="page-header">
        <div>
          <h1 className="page-title">API Explorer</h1>
          <p className="page-subtitle">Drive any Lattice endpoint from the dashboard using the live OpenAPI document, recent request history, and response diagnostics modeled after the Conductor explorer flow.</p>
        </div>
        <div className="page-actions">
          <button className="btn btn-secondary" onClick={resetRequest} disabled={!selectedOperation}>
            Reset Request
          </button>
          <button className="btn btn-secondary" onClick={refreshSpec} disabled={refreshing}>
            {refreshing ? 'Refreshing...' : 'Refresh OpenAPI'}
          </button>
        </div>
      </div>

      <div className="api-explorer-overview">
        <div className="card api-overview-card">
          <span className="api-overview-label">Server</span>
          <strong className="api-overview-value">{serverUrl}</strong>
        </div>
        <div className="card api-overview-card">
          <span className="api-overview-label">Operations</span>
          <strong className="api-overview-value">{operations.length}</strong>
        </div>
        <div className="card api-overview-card">
          <span className="api-overview-label">Recent Requests</span>
          <strong className="api-overview-value">{history.length}</strong>
        </div>
      </div>

      <div className="api-explorer-layout">
        <section className="api-explorer-request">
          <div className="card api-explorer-card">
            <div className="api-explorer-card-header">
              <div>
                <h2>Request Builder</h2>
                <p>Choose an operation, fill route/query values, and send the request directly against the connected server.</p>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Operation</label>
              <select
                className="input"
                value={selectedOperationKey}
                onChange={(event) => setSelectedOperationKey(event.target.value)}
              >
                {Object.entries(groupedOperations).map(([tag, tagOperations]) => (
                  <optgroup key={tag} label={tag}>
                    {tagOperations.map((operation) => (
                      <option key={operation.key} value={operation.key}>
                        {operation.method} {operation.path} - {operation.summary}
                      </option>
                    ))}
                  </optgroup>
                ))}
              </select>
            </div>

            {selectedOperation ? (
              <>
                <div className="api-operation-summary">
                  <span className={`api-method method-${selectedOperation.method.toLowerCase()}`}>{selectedOperation.method}</span>
                  <code>{selectedOperation.path}</code>
                </div>
                {selectedOperation.description ? (
                  <p className="api-operation-description">{selectedOperation.description}</p>
                ) : null}

                {pathParameters.length > 0 ? (
                  <div className="api-parameter-section">
                    <div className="api-section-heading">
                      <h3>Path Parameters</h3>
                    </div>
                    <div className="api-parameter-grid">
                      {pathParameters.map((parameter) => (
                        <div className="form-group" key={`path-${parameter.name}`}>
                          <label className="form-label">
                            {parameter.name}
                            {parameter.required ? ' *' : ''}
                          </label>
                          <input
                            className="input"
                            value={parameterValues[parameter.name] ?? ''}
                            onChange={(event) => updateParameterValue(parameter.name, event.target.value)}
                            placeholder={parameter.description || parameter.name}
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                ) : null}

                {queryParameters.length > 0 ? (
                  <div className="api-parameter-section">
                    <div className="api-section-heading">
                      <h3>Query Parameters</h3>
                    </div>
                    <div className="api-parameter-grid">
                      {queryParameters.map((parameter) => (
                        <div className="form-group" key={`query-${parameter.name}`}>
                          <label className="form-label">{parameter.name}</label>
                          <input
                            className="input"
                            value={parameterValues[parameter.name] ?? ''}
                            onChange={(event) => updateParameterValue(parameter.name, event.target.value)}
                            placeholder={parameter.description || parameter.name}
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                ) : null}

                <div className="api-parameter-section">
                  <div className="api-section-heading">
                    <h3>Custom Query Parameters</h3>
                  </div>
                  <KeyValueEditor
                    value={extraQueryValues}
                    onChange={setExtraQueryValues}
                    keyPlaceholder="Query key"
                    valuePlaceholder="Query value"
                    hint="Add extra query values beyond the generated OpenAPI parameter list."
                  />
                </div>

                <div className="api-parameter-section">
                  <div className="api-section-heading">
                    <h3>Headers</h3>
                  </div>
                  <KeyValueEditor
                    value={headerValues}
                    onChange={setHeaderValues}
                    keyPlaceholder="Header name"
                    valuePlaceholder="Header value"
                    hint="Add optional request headers such as Accept or custom diagnostic headers."
                  />
                </div>

                {selectedOperation.method !== 'GET' && selectedOperation.method !== 'HEAD' ? (
                  <div className="api-parameter-section">
                    <div className="api-section-heading">
                      <h3>Request Body</h3>
                      <button type="button" className="btn btn-secondary" onClick={() => setRequestBody(getRequestBodyTemplate(spec, selectedOperation))}>
                        Reset Template
                      </button>
                    </div>
                    <textarea
                      className="textarea api-editor"
                      value={requestBody}
                      onChange={(event) => setRequestBody(event.target.value)}
                      rows={16}
                      spellCheck={false}
                      placeholder="Enter request JSON"
                    />
                  </div>
                ) : (
                  <div className="api-empty-body">
                    <span>No request body is defined for this operation.</span>
                  </div>
                )}

                <div className="api-request-preview">
                  <div className="api-request-preview-header">
                    <strong>Resolved Request</strong>
                    <CopyButton value={`${serverUrl}${requestPath}`} title="Copy full request URL" />
                  </div>
                  <code>{requestPath || '/'}</code>
                </div>

                {sendError ? <div className="api-explorer-error">{sendError}</div> : null}

                <div className="api-request-actions">
                  <button className="btn btn-secondary" onClick={() => setResponse(null)} disabled={!response && !sendError}>
                    Clear Response
                  </button>
                  {sending ? (
                    <button className="btn btn-danger" onClick={() => abortControllerRef.current?.abort()}>
                      Cancel
                    </button>
                  ) : (
                    <button className="btn btn-primary" onClick={sendRequest}>
                      Send Request
                    </button>
                  )}
                </div>
              </>
            ) : null}
          </div>

          <div className="card api-explorer-card api-history-card">
            <div className="api-explorer-card-header">
              <div>
                <h2>Recent Requests</h2>
                <p>Reload a recent request configuration without leaving the explorer.</p>
              </div>
              <button className="btn btn-secondary" onClick={clearHistory} disabled={history.length === 0}>
                Clear
              </button>
            </div>

            {history.length === 0 ? (
              <div className="empty-state">
                <p>No requests sent from this browser yet.</p>
              </div>
            ) : (
              <div className="api-history-list">
                {history.map((item) => (
                  <button key={item.id} type="button" className="api-history-item" onClick={() => loadHistoryItem(item)}>
                    <div className="api-history-main">
                      <span className={`api-method method-${item.label.split(' ')[0].toLowerCase()}`}>{item.label.split(' ')[0]}</span>
                      <span className="api-history-path">{item.requestPath}</span>
                    </div>
                    <div className="api-history-meta">
                      <span>{formatDate(item.timestamp)}</span>
                      <span className={`history-status history-status-${item.status >= 400 ? 'error' : 'success'}`}>{item.status}</span>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </section>

        <section className="api-explorer-response">
          <div className="card api-explorer-card api-response-card">
            <div className="api-explorer-card-header">
              <div>
                <h2>Response</h2>
                <p>Preview parsed data, inspect raw payloads, and reuse the current request as curl, JavaScript, or C#.</p>
              </div>
            </div>

            <div className="api-response-summary">
              <div className="api-response-stat">
                <span className="api-response-stat-label">Status</span>
                <strong className={response ? `api-status api-status-${response.status >= 400 ? 'error' : 'success'}` : ''}>
                  {response ? `${response.status} ${response.statusText}` : '-'}
                </strong>
              </div>
              <div className="api-response-stat">
                <span className="api-response-stat-label">Duration</span>
                <strong>{response ? formatDuration(response.durationMs) : '-'}</strong>
              </div>
              <div className="api-response-stat">
                <span className="api-response-stat-label">Bytes</span>
                <strong>{response ? formatBytes(response.text?.length || 0) : '-'}</strong>
              </div>
              <div className="api-response-stat">
                <span className="api-response-stat-label">Request ID</span>
                <strong>{response?.requestId ? <CopyableId value={response.requestId} /> : '-'}</strong>
              </div>
            </div>

            <div className="api-response-tabs">
              {['preview', 'body', 'headers', 'status', 'code'].map((tab) => (
                <button
                  key={tab}
                  type="button"
                  className={`api-response-tab ${responseTab === tab ? 'active' : ''}`}
                  onClick={() => setResponseTab(tab)}
                >
                  {tab.charAt(0).toUpperCase() + tab.slice(1)}
                </button>
              ))}
            </div>

            <div className="api-response-panel">
              {responseTab === 'preview' ? (
                <pre className="api-response-pre">{prettyPrint(getResponsePreview(response))}</pre>
              ) : null}

              {responseTab === 'body' ? (
                <pre className="api-response-pre">{prettyPrint(getResponseBodyText(response))}</pre>
              ) : null}

              {responseTab === 'headers' ? (
                response ? (
                  <div className="api-header-table">
                    {Object.entries(response.headers).map(([key, value]) => (
                      <div key={key} className="api-header-row">
                        <span className="api-header-key">{key}</span>
                        <span className="api-header-value">{value}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="empty-state"><p>No response headers yet.</p></div>
                )
              ) : null}

              {responseTab === 'status' ? (
                <div className="api-status-panel">
                  <div className="api-status-grid">
                    <div className="api-status-card">
                      <span>HTTP Status</span>
                      <strong>{response ? response.status : '-'}</strong>
                    </div>
                    <div className="api-status-card">
                      <span>Status Text</span>
                      <strong>{response ? response.statusText || '(none)' : '-'}</strong>
                    </div>
                    <div className="api-status-card">
                      <span>Content Type</span>
                      <strong>{response ? response.contentType || '(none)' : '-'}</strong>
                    </div>
                    <div className="api-status-card">
                      <span>Duration</span>
                      <strong>{response ? formatDuration(response.durationMs) : '-'}</strong>
                    </div>
                  </div>
                  <div className="api-request-preview api-request-preview-secondary">
                    <div className="api-request-preview-header">
                      <strong>Effective URL</strong>
                    </div>
                    <code>{requestPath ? `${serverUrl}${requestPath}` : serverUrl}</code>
                  </div>
                </div>
              ) : null}

              {responseTab === 'code' ? (
                <div className="api-code-panel">
                  <div className="api-code-tabs">
                    {[
                      ['curl', 'curl'],
                      ['javascript', 'JavaScript'],
                      ['csharp', 'C#'],
                    ].map(([value, label]) => (
                      <button
                        key={value}
                        type="button"
                        className={`api-code-tab ${codeLanguage === value ? 'active' : ''}`}
                        onClick={() => setCodeLanguage(value)}
                      >
                        {label}
                      </button>
                    ))}
                  </div>
                  <div className="api-code-block">
                    <div className="api-request-preview-header">
                      <strong>{codeLanguage === 'curl' ? 'curl' : codeLanguage === 'javascript' ? 'JavaScript' : 'C#'}</strong>
                      <CopyButton value={codeSnippets[codeLanguage]} title="Copy code sample" />
                    </div>
                    <pre className="api-response-pre">{codeSnippets[codeLanguage] || '(select an operation)'}</pre>
                  </div>
                </div>
              ) : null}
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

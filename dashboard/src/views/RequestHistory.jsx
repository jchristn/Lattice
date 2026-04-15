import { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useApp } from '../context/AppContext'
import ActionMenu from '../components/ActionMenu'
import CopyButton from '../components/CopyButton'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import Modal from '../components/Modal'
import TablePagination from '../components/TablePagination'
import { formatDate } from '../utils/api'
import './RequestHistory.css'

const TIME_RANGES = [
  { label: 'Last Hour', value: 'hour', interval: 'minute', stepMs: 60_000, bucketCount: 60 },
  { label: 'Last Day', value: 'day', interval: '15minute', stepMs: 900_000, bucketCount: 96 },
  { label: 'Last Week', value: 'week', interval: 'hour', stepMs: 3_600_000, bucketCount: 24 * 7 },
  { label: 'Last Month', value: 'month', interval: '6hour', stepMs: 21_600_000, bucketCount: 4 * 30 },
]

const REQUEST_TYPES = ['healthCheck', 'collection', 'document', 'search']
const HTTP_METHODS = ['GET', 'PUT', 'POST', 'DELETE', 'HEAD']

function floorToStep(timestamp, stepMs) {
  return Math.floor(timestamp / stepMs) * stepMs
}

function buildBuckets(summary, range) {
  const apiBuckets = new Map(
    (summary?.data || []).map((bucket) => [
      floorToStep(new Date(bucket.timestampUtc).getTime(), range.stepMs),
      bucket,
    ])
  )

  const startMs = range.startUtc.getTime()

  return Array.from({ length: range.bucketCount }, (_, index) => {
    const timestamp = startMs + index * range.stepMs
    const apiBucket = apiBuckets.get(timestamp)
    return {
      timestampUtc: new Date(timestamp).toISOString(),
      successCount: apiBucket?.successCount || 0,
      failureCount: apiBucket?.failureCount || 0,
    }
  })
}

function formatChartLabel(timestamp, interval) {
  const date = new Date(timestamp)
  if (interval === 'day') {
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
  }
  if (interval === 'hour' || interval === '6hour') {
    return date.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
  }
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
}

function formatTooltipTimestamp(timestamp, interval) {
  const date = new Date(timestamp)
  if (interval === 'day') {
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
  }

  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatDuration(milliseconds) {
  if (!milliseconds && milliseconds !== 0) return '-'
  return `${milliseconds.toFixed(1)} ms`
}

function formatBytes(bytes) {
  if (!bytes) return '0 B'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function normalizeFilters(filters) {
  return {
    ...filters,
    statusCode: filters.statusCode ? Number.parseInt(filters.statusCode, 10) : null,
    success: filters.success === '' ? null : filters.success === 'true',
    startUtc: filters.startUtc ? new Date(filters.startUtc).toISOString() : null,
    endUtc: filters.endUtc ? new Date(filters.endUtc).toISOString() : null,
  }
}

function createEmptyFilters() {
  return {
    requestType: '',
    method: '',
    pathContains: '',
    collectionId: '',
    documentId: '',
    schemaId: '',
    tableName: '',
    sourceIp: '',
    statusCode: '',
    success: '',
    startUtc: '',
    endUtc: '',
  }
}

function toDateTimeLocal(value) {
  if (!value) return ''
  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  const localDate = new Date(date.getTime() - offset * 60_000)
  return localDate.toISOString().slice(0, 16)
}

function getQuickRange(rangeValue) {
  const range = TIME_RANGES.find((entry) => entry.value === rangeValue) || TIME_RANGES[1]
  const endExclusiveMs = floorToStep(Date.now(), range.stepMs) + range.stepMs
  const startMs = endExclusiveMs - range.bucketCount * range.stepMs

  return {
    ...range,
    startUtc: new Date(startMs),
    endUtc: new Date(endExclusiveMs - 1),
  }
}

function formatRequestType(value) {
  if (!value) return '-'
  return value.charAt(0).toUpperCase() + value.slice(1).replace(/([A-Z])/g, ' $1')
}

function CollapsibleBlock({ title, value, defaultExpanded = false }) {
  const [expanded, setExpanded] = useState(defaultExpanded)
  const textValue = typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)

  return (
    <div className="request-history-collapsible">
      <button type="button" className="request-history-collapsible-header" onClick={() => setExpanded((current) => !current)}>
        <span>{title}</span>
        <span>{expanded ? 'Hide' : 'Show'}</span>
      </button>
      {expanded ? (
        <div className="request-history-collapsible-body">
          <div className="request-history-collapsible-toolbar">
            <span>Content</span>
            <CopyButton value={textValue} title={`Copy ${title}`} />
          </div>
          <pre>{textValue || '(empty)'}</pre>
        </div>
      ) : null}
    </div>
  )
}

function SummaryChart({ summary, timeRange }) {
  const range = getQuickRange(timeRange)
  const buckets = buildBuckets(summary, range)
  const [hoveredBucket, setHoveredBucket] = useState(null)
  const [tooltipPosition, setTooltipPosition] = useState(null)
  const tooltipRef = useRef(null)
  const maxCount = Math.max(1, ...buckets.map((bucket) => bucket.successCount + bucket.failureCount))

  useEffect(() => {
    if (!hoveredBucket || !tooltipRef.current) {
      setTooltipPosition(null)
      return
    }

    const tooltipRect = tooltipRef.current.getBoundingClientRect()
    const viewportPadding = 12
    const pointerOffset = 16
    let left = hoveredBucket.clientX + pointerOffset
    let top = hoveredBucket.clientY - tooltipRect.height - pointerOffset

    if (left + tooltipRect.width + viewportPadding > window.innerWidth) {
      left = hoveredBucket.clientX - tooltipRect.width - pointerOffset
    }

    if (top < viewportPadding) {
      top = hoveredBucket.clientY + pointerOffset
    }

    left = Math.max(viewportPadding, Math.min(left, window.innerWidth - tooltipRect.width - viewportPadding))
    top = Math.max(viewportPadding, Math.min(top, window.innerHeight - tooltipRect.height - viewportPadding))

    setTooltipPosition((current) => {
      if (current && current.left === left && current.top === top) {
        return current
      }

      return { left, top }
    })
  }, [hoveredBucket])

  if (!buckets.length) {
    return (
      <div className="request-history-chart-empty">
        <p>No request history data is available for the selected range.</p>
      </div>
    )
  }

  const chartHeight = 240
  const chartWidth = 900
  const paddingLeft = 48
  const paddingRight = 24
  const paddingTop = 20
  const paddingBottom = 42
  const innerWidth = chartWidth - paddingLeft - paddingRight
  const innerHeight = chartHeight - paddingTop - paddingBottom
  const barWidth = Math.max(8, innerWidth / Math.max(buckets.length, 1) - 4)

  return (
    <>
      <div className="request-history-chart-wrap">
        <svg viewBox={`0 0 ${chartWidth} ${chartHeight}`} className="request-history-chart">
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const y = paddingTop + innerHeight - innerHeight * ratio
          const label = Math.round(maxCount * ratio)
          return (
            <g key={ratio}>
              <line x1={paddingLeft} x2={chartWidth - paddingRight} y1={y} y2={y} className="request-history-grid-line" />
              <text x={paddingLeft - 10} y={y + 4} textAnchor="end" className="request-history-axis-label">
                {label}
              </text>
            </g>
          )
        })}

        {buckets.map((bucket, index) => {
          const total = bucket.successCount + bucket.failureCount
          const x = paddingLeft + index * (innerWidth / Math.max(buckets.length, 1)) + 2
          const successHeight = innerHeight * (bucket.successCount / maxCount)
          const failureHeight = innerHeight * (bucket.failureCount / maxCount)
          const totalHeight = innerHeight * (total / maxCount)
          const y = paddingTop + innerHeight - totalHeight

          return (
            <g
              key={`${bucket.timestampUtc}-${index}`}
              onMouseEnter={(event) => {
                setHoveredBucket({
                  ...bucket,
                  total,
                  clientX: event.clientX,
                  clientY: event.clientY,
                })
              }}
              onMouseMove={(event) => {
                setHoveredBucket({
                  ...bucket,
                  total,
                  clientX: event.clientX,
                  clientY: event.clientY,
                })
              }}
              onMouseLeave={() => setHoveredBucket(null)}
            >
              <rect
                x={x}
                y={paddingTop}
                width={barWidth}
                height={innerHeight}
                className="request-history-bar-hitbox"
              />
              <rect
                x={x}
                y={paddingTop + innerHeight - successHeight}
                width={barWidth}
                height={successHeight}
                className="request-history-bar-success"
                rx="3"
              />
              <rect
                x={x}
                y={y}
                width={barWidth}
                height={failureHeight}
                className="request-history-bar-failure"
                rx="3"
              />
              {index === 0 || index === buckets.length - 1 || index % Math.ceil(buckets.length / 5) === 0 ? (
                <text
                  x={x + barWidth / 2}
                  y={chartHeight - 16}
                  textAnchor="middle"
                  className="request-history-axis-label"
                >
                  {formatChartLabel(bucket.timestampUtc, range.interval)}
                </text>
              ) : null}
            </g>
          )
        })}
        </svg>
      </div>
      {hoveredBucket && typeof document !== 'undefined'
        ? createPortal(
            <div
              ref={tooltipRef}
              className="request-history-chart-tooltip"
              style={tooltipPosition ? { left: tooltipPosition.left, top: tooltipPosition.top } : { left: -9999, top: -9999 }}
            >
              <strong>{formatTooltipTimestamp(hoveredBucket.timestampUtc, range.interval)}</strong>
              <span>Total: {hoveredBucket.total.toLocaleString()}</span>
              <span>Success: {hoveredBucket.successCount.toLocaleString()}</span>
              <span>Failed: {hoveredBucket.failureCount.toLocaleString()}</span>
            </div>,
            document.body
          )
        : null}
    </>
  )
}

export default function RequestHistory() {
  const { api, setError } = useApp()
  const [filters, setFilters] = useState(() => createEmptyFilters())
  const [appliedFilters, setAppliedFilters] = useState(() => normalizeFilters(createEmptyFilters()))
  const [timeRange, setTimeRange] = useState('day')
  const [entries, setEntries] = useState([])
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [summaryLoading, setSummaryLoading] = useState(true)
  const [refreshToken, setRefreshToken] = useState(0)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [selectedEntry, setSelectedEntry] = useState(null)
  const [detail, setDetail] = useState(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [showDetailModal, setShowDetailModal] = useState(false)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })

  const activeRange = useMemo(() => getQuickRange(timeRange), [timeRange])

  const summaryCounts = {
    total: summary?.totalRequests || 0,
    success: summary?.totalSuccess || 0,
    failure: summary?.totalFailure || 0,
  }

  useEffect(() => {
    const fetchEntries = async () => {
      try {
        setLoading(true)
        const result = await api.searchRequestHistory({
          ...Object.fromEntries(Object.entries(appliedFilters).filter(([, value]) => value !== null && value !== '' && value !== undefined)),
          page: page + 1,
          pageSize,
        })
        setEntries(result.data || [])
        setTotalCount(result.totalCount || 0)
        setTotalPages(result.totalPages || 1)
      } catch (err) {
        setError('Failed to load request history: ' + err.message)
      } finally {
        setLoading(false)
      }
    }

    fetchEntries()
  }, [api, appliedFilters, page, pageSize, refreshToken, setError])

  useEffect(() => {
    const fetchSummary = async () => {
      try {
        setSummaryLoading(true)
        const summaryParams = {
          ...Object.fromEntries(Object.entries(appliedFilters).filter(([key, value]) => !['page', 'pageSize'].includes(key) && value !== null && value !== '' && value !== undefined)),
          interval: activeRange.interval,
        }

        if (!summaryParams.startUtc) {
          summaryParams.startUtc = activeRange.startUtc.toISOString()
        }
        if (!summaryParams.endUtc) {
          summaryParams.endUtc = activeRange.endUtc.toISOString()
        }

        const result = await api.getRequestHistorySummary(summaryParams)
        setSummary(result)
      } catch (err) {
        setError('Failed to load request history summary: ' + err.message)
      } finally {
        setSummaryLoading(false)
      }
    }

    fetchSummary()
  }, [activeRange, api, appliedFilters, refreshToken, setError])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const applyFilters = () => {
    setAppliedFilters(normalizeFilters(filters))
    setPage(0)
  }

  const clearFilters = () => {
    const empty = createEmptyFilters()
    setFilters(empty)
    setAppliedFilters(normalizeFilters(empty))
    setPage(0)
  }

  const applyQuickRange = (rangeValue) => {
    setTimeRange(rangeValue)
    const range = getQuickRange(rangeValue)
    setFilters((current) => ({
      ...current,
      startUtc: toDateTimeLocal(range.startUtc),
      endUtc: toDateTimeLocal(range.endUtc),
    }))
  }

  const loadDetail = async (entry, openJson = false) => {
    try {
      setSelectedEntry(entry)
      setShowDetailModal(!openJson)
      setDetailLoading(true)
      const detailData = await api.getRequestHistoryDetail(entry.id)
      setDetail(detailData)

      if (openJson) {
        setJsonViewer({
          open: true,
          title: 'Request History JSON',
          subtitle: 'Full request and response detail captured by the server for this API request.',
          identifier: entry.id,
          value: detailData,
        })
      }
    } catch (err) {
      setError('Failed to load request history detail: ' + err.message)
    } finally {
      setDetailLoading(false)
    }
  }

  const deleteEntry = async (entry) => {
    if (!window.confirm(`Delete request history entry ${entry.id}?`)) {
      return
    }

    try {
      await api.deleteRequestHistoryEntry(entry.id)
      if (selectedEntry?.id === entry.id) {
        setShowDetailModal(false)
        setSelectedEntry(null)
        setDetail(null)
      }
      setRefreshToken((current) => current + 1)
    } catch (err) {
      setError('Failed to delete request history entry: ' + err.message)
    }
  }

  const bulkDelete = async () => {
    if (!window.confirm('Delete all request history entries matching the current filters?')) {
      return
    }

    try {
      await api.bulkDeleteRequestHistory(appliedFilters)
      setShowDetailModal(false)
      setSelectedEntry(null)
      setDetail(null)
      setPage(0)
      setRefreshToken((current) => current + 1)
    } catch (err) {
      setError('Failed to bulk delete request history: ' + err.message)
    }
  }

  return (
    <div className="request-history">
      <div className="page-header">
        <div>
          <h1 className="page-title">Request History</h1>
          <p className="page-subtitle">Inspect captured request metadata, timings, headers, and bodies from the Lattice server with a dashboard flow aligned to Conductor's history and chart experience.</p>
        </div>
        <div className="page-actions">
          <button className="btn btn-secondary" onClick={() => setRefreshToken((current) => current + 1)}>
            Refresh
          </button>
          <button className="btn btn-danger" onClick={bulkDelete} disabled={totalCount === 0}>
            Delete Matching
          </button>
        </div>
      </div>

      <div className="card request-history-summary-card">
        <div className="request-history-summary-header">
          <div>
            <h2>Traffic Summary</h2>
            <p>Use the same time-range workflow as Conductor to quickly spot spikes, failures, and quiet periods.</p>
          </div>
          <div className="request-history-time-tabs">
            {TIME_RANGES.map((range) => (
              <button
                key={range.value}
                type="button"
                className={`request-history-time-tab ${timeRange === range.value ? 'active' : ''}`}
                onClick={() => applyQuickRange(range.value)}
              >
                {range.label}
              </button>
            ))}
          </div>
        </div>

        <div className="request-history-summary-stats">
          <div className="request-history-stat-card">
            <span>Total Requests</span>
            <strong>{summaryCounts.total.toLocaleString()}</strong>
          </div>
          <div className="request-history-stat-card">
            <span>Success</span>
            <strong className="request-history-success">{summaryCounts.success.toLocaleString()}</strong>
          </div>
          <div className="request-history-stat-card">
            <span>Failed</span>
            <strong className="request-history-failure">{summaryCounts.failure.toLocaleString()}</strong>
          </div>
        </div>

        {summaryLoading ? (
          <div className="request-history-chart-empty"><p>Loading summary...</p></div>
        ) : (
          <SummaryChart summary={summary} timeRange={timeRange} />
        )}
      </div>

      <div className="card request-history-filters-card">
        <div className="request-history-filters-header">
          <div>
            <h2>Filters</h2>
            <p>Narrow the table and chart to a route, collection, document, status, or time window.</p>
          </div>
        </div>

        <div className="request-history-filters-grid">
          <div className="form-group">
            <label className="form-label">Request Type</label>
            <select className="input" value={filters.requestType} onChange={(event) => setFilters((current) => ({ ...current, requestType: event.target.value }))}>
              <option value="">All</option>
              {REQUEST_TYPES.map((value) => (
                <option key={value} value={value}>{formatRequestType(value)}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Method</label>
            <select className="input" value={filters.method} onChange={(event) => setFilters((current) => ({ ...current, method: event.target.value }))}>
              <option value="">All</option>
              {HTTP_METHODS.map((value) => (
                <option key={value} value={value}>{value}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Status Code</label>
            <input className="input" value={filters.statusCode} onChange={(event) => setFilters((current) => ({ ...current, statusCode: event.target.value.replace(/[^\d]/g, '') }))} placeholder="200" />
          </div>

          <div className="form-group">
            <label className="form-label">Success</label>
            <select className="input" value={filters.success} onChange={(event) => setFilters((current) => ({ ...current, success: event.target.value }))}>
              <option value="">All</option>
              <option value="true">Success</option>
              <option value="false">Failed</option>
            </select>
          </div>

          <div className="form-group request-history-filter-wide">
            <label className="form-label">Path Contains</label>
            <input className="input" value={filters.pathContains} onChange={(event) => setFilters((current) => ({ ...current, pathContains: event.target.value }))} placeholder="/v1.0/collections" />
          </div>

          <div className="form-group">
            <label className="form-label">Collection ID</label>
            <input className="input" value={filters.collectionId} onChange={(event) => setFilters((current) => ({ ...current, collectionId: event.target.value }))} placeholder="Collection ID" />
          </div>

          <div className="form-group">
            <label className="form-label">Document ID</label>
            <input className="input" value={filters.documentId} onChange={(event) => setFilters((current) => ({ ...current, documentId: event.target.value }))} placeholder="Document ID" />
          </div>

          <div className="form-group">
            <label className="form-label">Schema ID</label>
            <input className="input" value={filters.schemaId} onChange={(event) => setFilters((current) => ({ ...current, schemaId: event.target.value }))} placeholder="Schema ID" />
          </div>

          <div className="form-group">
            <label className="form-label">Table Name</label>
            <input className="input" value={filters.tableName} onChange={(event) => setFilters((current) => ({ ...current, tableName: event.target.value }))} placeholder="Index table" />
          </div>

          <div className="form-group">
            <label className="form-label">Source IP</label>
            <input className="input" value={filters.sourceIp} onChange={(event) => setFilters((current) => ({ ...current, sourceIp: event.target.value }))} placeholder="127.0.0.1" />
          </div>

          <div className="form-group">
            <label className="form-label">Start UTC</label>
            <input className="input" type="datetime-local" value={filters.startUtc} onChange={(event) => setFilters((current) => ({ ...current, startUtc: event.target.value }))} />
          </div>

          <div className="form-group">
            <label className="form-label">End UTC</label>
            <input className="input" type="datetime-local" value={filters.endUtc} onChange={(event) => setFilters((current) => ({ ...current, endUtc: event.target.value }))} />
          </div>
        </div>

        <div className="request-history-filter-actions">
          <button className="btn btn-secondary" onClick={clearFilters}>Clear</button>
          <button className="btn btn-primary" onClick={applyFilters}>Apply Filters</button>
        </div>
      </div>

      <div className="card">
        <div className="table-results-count">
          Showing {entries.length} of {totalCount.toLocaleString()} captured request{totalCount === 1 ? '' : 's'}
        </div>
        <TablePagination
          totalRecords={totalCount}
          currentPage={page}
          totalPages={totalPages}
          onPageChange={setPage}
          onRefresh={() => setRefreshToken((current) => current + 1)}
          disabled={loading}
          pageSize={pageSize}
          onPageSizeChange={(value) => {
            setPageSize(value)
            setPage(0)
          }}
        />

        <table className="table request-history-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Method</th>
              <th>Path</th>
              <th>Status</th>
              <th>Type</th>
              <th>Collection</th>
              <th>Duration</th>
              <th>Request ID</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={9} className="empty-row">Loading request history...</td>
              </tr>
            ) : entries.length === 0 ? (
              <tr>
                <td colSpan={9} className="empty-row">No requests match the current filters.</td>
              </tr>
            ) : (
              entries.map((entry) => (
                <tr
                  key={entry.id}
                  className="request-history-row"
                  onClick={() => loadDetail(entry)}
                >
                  <td>{formatDate(entry.createdUtc)}</td>
                  <td><span className={`request-history-method request-history-method-${entry.method.toLowerCase()}`}>{entry.method}</span></td>
                  <td>
                    <div className="request-history-path-cell">
                      <code>{entry.path}</code>
                      {entry.documentId ? <span className="request-history-secondary-id">Doc: {entry.documentId}</span> : null}
                    </div>
                  </td>
                  <td><span className={`request-history-status request-history-status-${entry.statusCode >= 400 ? 'error' : 'success'}`}>{entry.statusCode}</span></td>
                  <td>{formatRequestType(entry.requestType)}</td>
                  <td onClick={(e) => e.stopPropagation()}>{entry.collectionId ? <CopyableId value={entry.collectionId} /> : '-'}</td>
                  <td>{formatDuration(entry.processingTimeMs)}</td>
                  <td onClick={(e) => e.stopPropagation()}><CopyableId value={entry.id} /></td>
                  <td onClick={(e) => e.stopPropagation()}>
                    <ActionMenu
                      items={[
                        { label: 'View Detail', onClick: () => loadDetail(entry) },
                        { label: 'View JSON', onClick: () => loadDetail(entry, true) },
                        { label: 'Delete Entry', onClick: () => deleteEntry(entry), variant: 'danger' },
                      ]}
                    />
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal
        isOpen={showDetailModal}
        onClose={() => {
          setShowDetailModal(false)
          setSelectedEntry(null)
        }}
        title="Request Detail"
        subtitle="Review the full server-side request and response capture, including headers, body snapshots, and timing."
        extraWide
      >
        {detailLoading ? (
          <div className="loading">Loading request detail...</div>
        ) : detail ? (
          <div className="request-history-detail">
            <div className="request-history-detail-grid">
              <div className="request-history-detail-item">
                <span>Request ID</span>
                <strong><CopyableId value={detail.id} /></strong>
              </div>
              <div className="request-history-detail-item">
                <span>Status</span>
                <strong>{detail.statusCode}</strong>
              </div>
              <div className="request-history-detail-item">
                <span>Method</span>
                <strong>{detail.method}</strong>
              </div>
              <div className="request-history-detail-item">
                <span>Request Type</span>
                <strong>{formatRequestType(detail.requestType)}</strong>
              </div>
              <div className="request-history-detail-item">
                <span>Duration</span>
                <strong>{formatDuration(detail.processingTimeMs)}</strong>
              </div>
              <div className="request-history-detail-item">
                <span>Response Size</span>
                <strong>{formatBytes(detail.responseBodyLength)}</strong>
              </div>
            </div>

            <div className="request-history-detail-url">
              <span>URL</span>
              <code>{detail.url}</code>
            </div>

            <CollapsibleBlock title="Request Headers" value={detail.requestHeaders} />
            <CollapsibleBlock title="Request Body" value={detail.requestBody || ''} />
            <CollapsibleBlock title="Response Headers" value={detail.responseHeaders} />
            <CollapsibleBlock title="Response Body" value={detail.responseBody || ''} />
          </div>
        ) : (
          <div className="empty-state"><p>No detail is available for this request.</p></div>
        )}
      </Modal>

      <JsonViewerModal
        isOpen={jsonViewer.open}
        onClose={() => setJsonViewer({ open: false, title: '', subtitle: '', identifier: '', value: null })}
        title={jsonViewer.title}
        subtitle={jsonViewer.subtitle}
        identifier={jsonViewer.identifier}
        value={jsonViewer.value}
      />
    </div>
  )
}

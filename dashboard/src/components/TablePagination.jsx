import { useEffect, useState } from 'react'
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  ChevronsLeftIcon,
  ChevronsRightIcon,
  RefreshIcon,
} from './Icons'
import './TablePagination.css'

const DEFAULT_PAGE_SIZE_OPTIONS = [10, 25, 50, 100, 250, 500, 1000]

export default function TablePagination({
  totalRecords,
  currentPage,
  totalPages,
  onPageChange,
  onRefresh,
  disabled = false,
  pageSizeLabel,
  pageSize,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZE_OPTIONS,
}) {
  const [pageInput, setPageInput] = useState(String(currentPage + 1))

  useEffect(() => {
    setPageInput(String(currentPage + 1))
  }, [currentPage])

  const canNavigate = !disabled && totalPages > 0

  const submitPage = () => {
    if (!canNavigate) return

    const parsed = Number.parseInt(pageInput, 10)
    if (Number.isNaN(parsed)) {
      setPageInput(String(currentPage + 1))
      return
    }

    const nextPage = Math.min(Math.max(parsed, 1), totalPages) - 1
    onPageChange(nextPage)
  }

  return (
    <div className="table-pagination" role="group" aria-label="Table pagination controls">
      <div className="table-pagination-summary">
        <span className="table-pagination-total">
          Total records: <strong>{totalRecords.toLocaleString()}</strong>
        </span>
        {pageSizeLabel ? <span className="table-pagination-size">{pageSizeLabel}</span> : null}
        {onPageSizeChange ? (
          <label className="table-pagination-page-size">
            <span>Per page</span>
            <select
              value={pageSize}
              onChange={(event) => onPageSizeChange(Number.parseInt(event.target.value, 10))}
              disabled={disabled}
            >
              {pageSizeOptions.map((option) => (
                <option key={option} value={option}>
                  {option.toLocaleString()}
                </option>
              ))}
            </select>
          </label>
        ) : null}
      </div>

      <div className="table-pagination-controls">
        <button type="button" className="table-pagination-btn" onClick={() => onPageChange(0)} disabled={!canNavigate || currentPage === 0} aria-label="First page">
          <ChevronsLeftIcon size={14} />
          <span>&lt;&lt;</span>
        </button>
        <button type="button" className="table-pagination-btn" onClick={() => onPageChange(currentPage - 1)} disabled={!canNavigate || currentPage === 0} aria-label="Previous page">
          <ChevronLeftIcon size={14} />
          <span>&lt;</span>
        </button>
        <label className="table-pagination-jump">
          <span>Page</span>
          <input
            type="text"
            inputMode="numeric"
            value={pageInput}
            onChange={(event) => setPageInput(event.target.value.replace(/[^\d]/g, ''))}
            onBlur={submitPage}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                submitPage()
              }
            }}
            disabled={!canNavigate}
          />
          <span>of {Math.max(totalPages, 1).toLocaleString()}</span>
        </label>
        <button type="button" className="table-pagination-btn" onClick={() => onPageChange(currentPage + 1)} disabled={!canNavigate || currentPage >= totalPages - 1} aria-label="Next page">
          <span>&gt;</span>
          <ChevronRightIcon size={14} />
        </button>
        <button type="button" className="table-pagination-btn" onClick={() => onPageChange(totalPages - 1)} disabled={!canNavigate || currentPage >= totalPages - 1} aria-label="Last page">
          <span>&gt;&gt;</span>
          <ChevronsRightIcon size={14} />
        </button>
        <button type="button" className="table-pagination-btn table-pagination-refresh" onClick={onRefresh} disabled={disabled} aria-label="Refresh table">
          <RefreshIcon size={14} />
        </button>
      </div>
    </div>
  )
}

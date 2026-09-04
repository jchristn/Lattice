import { useCallback, useState } from 'react'

// Persists a table's "per page" selection in localStorage so the choice
// survives navigating away and back (and full reloads). Each table passes a
// distinct `key` so tables remember their own selection independently.
export default function usePersistedPageSize(key, defaultValue = 25) {
  const storageKey = `lattice.pageSize.${key}`

  const [pageSize, setPageSizeState] = useState(() => {
    try {
      const stored = window.localStorage.getItem(storageKey)
      const parsed = stored != null ? Number.parseInt(stored, 10) : Number.NaN
      return Number.isFinite(parsed) && parsed > 0 ? parsed : defaultValue
    } catch {
      return defaultValue
    }
  })

  const setPageSize = useCallback(
    (value) => {
      setPageSizeState(value)
      try {
        window.localStorage.setItem(storageKey, String(value))
      } catch {
        // Ignore storage failures (private mode, quota, disabled storage).
      }
    },
    [storageKey],
  )

  return [pageSize, setPageSize]
}

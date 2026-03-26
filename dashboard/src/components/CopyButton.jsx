import { useState } from 'react'
import { copyToClipboard } from '../utils/clipboard'
import { CheckIcon, CopyIcon } from './Icons'
import './CopyButton.css'

export default function CopyButton({ value, className = '', title = 'Copy to clipboard', size = 16, onCopied }) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    try {
      await copyToClipboard(value)
      setCopied(true)
      onCopied?.()
      window.setTimeout(() => setCopied(false), 1600)
    } catch (err) {
      console.error('Failed to copy:', err)
    }
  }

  return (
    <button
      type="button"
      className={`copy-button ${copied ? 'is-copied' : ''} ${className}`.trim()}
      onClick={handleCopy}
      title={copied ? 'Copied' : title}
      aria-label={copied ? 'Copied' : title}
    >
      {copied ? <CheckIcon size={size} /> : <CopyIcon size={size} />}
    </button>
  )
}

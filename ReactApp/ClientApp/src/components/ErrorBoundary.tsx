import { Component, ErrorInfo, ReactNode } from 'react'

/**
 * Minimal error boundary fallback - plain HTML only, no Ant Design.
 * Uses CSS variables from index.css (--moba-error-*) so colors are design tokens, not hardcoded.
 * Use when the outer boundary's fallback might throw (e.g. broken theme/context).
 */
function MinimalErrorFallback({
  error,
  errorInfo,
  onRetry,
}: {
  error: Error
  errorInfo: ErrorInfo | null
  onRetry: () => void
}) {
  return (
    <div style={{ padding: 24, fontFamily: 'sans-serif' }}>
      <h2 style={{ color: 'var(--moba-error-title)', marginBottom: 12 }}>Fehler in der Anwendung</h2>
      <p style={{ marginBottom: 8 }}>Die Seite wurde wegen eines Fehlers zurückgesetzt. Details in der Browser-Konsole (F12).</p>
      <pre
        style={{
          background: 'var(--moba-error-bg)',
          padding: 12,
          borderRadius: 4,
          fontSize: 12,
          overflow: 'auto',
          maxHeight: 200,
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
        }}
      >
        {String(error.message)}
        {errorInfo?.componentStack && (
          <span style={{ display: 'block', marginTop: 8 }}>{errorInfo.componentStack}</span>
        )}
      </pre>
      <button
        type="button"
        onClick={onRetry}
        style={{ marginTop: 12, padding: '8px 16px', cursor: 'pointer' }}
      >
        Erneut versuchen
      </button>
    </div>
  )
}

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
  error: Error | null
  errorInfo: ErrorInfo | null
}

/**
 * Error boundary to catch runtime errors and display a fallback UI
 * instead of a blank page. Logs error details for debugging.
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    }
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    this.setState({ errorInfo })
    console.error('[ErrorBoundary] Uncaught error:', error, errorInfo)
  }

  handleRetry = (): void => {
    this.setState({ hasError: false, error: null, errorInfo: null })
  }

  render(): ReactNode {
    if (this.state.hasError && this.state.error) {
      return (
        <MinimalErrorFallback
          error={this.state.error}
          errorInfo={this.state.errorInfo}
          onRetry={this.handleRetry}
        />
      )
    }
    return this.props.children
  }
}

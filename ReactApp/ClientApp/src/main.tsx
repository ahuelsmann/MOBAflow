import React from 'react'
import ReactDOM from 'react-dom/client'
import { ConfigProvider } from 'antd'
import deDE from 'antd/locale/de_DE'
import App from './App'
import { StoreProvider } from './stores/StoreContext'
import { ErrorBoundary } from './components/ErrorBoundary'
import './index.css'

// Log unhandled promise rejections (errors in async code are not caught by Error Boundaries)
window.addEventListener('unhandledrejection', (event) => {
  console.error('[MOBAflow] Unhandled promise rejection:', event.reason)
})

// Log any uncaught error so we can see what causes the blank page
window.addEventListener('error', (event) => {
  console.error('[MOBAflow] Global error:', event.message, event.filename, event.lineno, event.colno, event.error)
})

// StrictMode is enabled. In React 18 dev it double-invokes effects; DashboardPage defers initial
// fetch with setTimeout(0) and clears it in cleanup so that double mount/unmount does not cause
// a blank page or setState-on-unmounted warnings.
ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ErrorBoundary>
      <ConfigProvider
        locale={deDE}
        theme={{
          token: {
            colorPrimary: '#667eea',
            borderRadius: 6,
          },
        }}
      >
        <StoreProvider>
          <App />
        </StoreProvider>
      </ConfigProvider>
    </ErrorBoundary>
  </React.StrictMode>,
)

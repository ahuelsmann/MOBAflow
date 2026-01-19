import React from 'react'
import ReactDOM from 'react-dom/client'
import { ConfigProvider } from 'antd'
import deDE from 'antd/locale/de_DE'
import App from './App'
import { StoreProvider } from './stores/StoreContext'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
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
  </React.StrictMode>,
)

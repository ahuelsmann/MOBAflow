import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Layout } from 'antd'
import AppHeader from './components/AppHeader'
import AppSidebar from './components/AppSidebar'
import DashboardPage from './pages/DashboardPage'
import TrainsPage from './pages/TrainsPage'
import JourneysPage from './pages/JourneysPage'
import SettingsPage from './pages/SettingsPage'
import './App.css'

const { Content } = Layout

function App() {
  return (
    <BrowserRouter>
      <Layout style={{ minHeight: '100vh' }}>
        <AppHeader />
        <Layout>
          <AppSidebar />
          <Layout style={{ padding: '24px' }}>
            <Content
              style={{
                padding: 24,
                margin: 0,
                minHeight: 280,
                background: '#fff',
                borderRadius: 8,
              }}
            >
              <Routes>
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/trains" element={<TrainsPage />} />
                <Route path="/journeys" element={<JourneysPage />} />
                <Route path="/settings" element={<SettingsPage />} />
              </Routes>
            </Content>
          </Layout>
        </Layout>
      </Layout>
    </BrowserRouter>
  )
}

export default App

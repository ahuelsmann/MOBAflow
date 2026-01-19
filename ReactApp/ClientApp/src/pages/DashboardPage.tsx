import { useEffect } from 'react'
import { Row, Col, Card, Statistic, Button, Space, Alert, Select, Tag, Progress, Empty } from 'antd'
import {
  ApiOutlined,
  ThunderboltOutlined,
  PoweroffOutlined,
  WarningOutlined,
  ReloadOutlined,
  DisconnectOutlined,
} from '@ant-design/icons'
import { observer } from 'mobx-react-lite'
import { useStore } from '../stores/StoreContext'
import type { TrackStatistic } from '../stores/AppStore'

/**
 * Dashboard Page - React equivalent of WebApp/Dashboard.razor
 * 
 * Direct comparison:
 * - Blazor: Uses @inject WebAppViewModel + PropertyChanged events
 * - React: Uses MobX observer + AppStore with polling
 */
const DashboardPage = observer(() => {
  const { appStore } = useStore()

  useEffect(() => {
    // Initial data load
    appStore.fetchSystemState()
    appStore.fetchStatistics()
    appStore.fetchLapCounterSettings()

    // Start polling if connected (similar to Blazor SignalR real-time updates)
    if (appStore.isConnected) {
      appStore.startPolling()
    }

    return () => {
      appStore.stopPolling()
    }
  }, [appStore])

  return (
    <div className="dashboard-page">
      <h1 style={{ marginBottom: 24 }}>📊 MOBAdash</h1>

      {appStore.connectionError && (
        <Alert
          message="Verbindungsfehler"
          description={appStore.connectionError}
          type="error"
          showIcon
          closable
          style={{ marginBottom: 24 }}
        />
      )}

      {/* ════════════════════════════════════════════════════════════════════
          DASHBOARD WIDGETS (matches dashboard-widgets in Blazor)
          ════════════════════════════════════════════════════════════════════ */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        {/* Connection Widget - matches ConnectionWidget.razor */}
        <Col xs={24} md={8}>
          <Card title="Z21 Connection" size="small">
            <Space direction="vertical" style={{ width: '100%' }}>
              <div>
                <label style={{ display: 'block', marginBottom: 4, fontSize: 12, color: '#666' }}>
                  Z21 IP Address:
                </label>
                <Select
                  style={{ width: '100%' }}
                  value={appStore.ipAddress}
                  onChange={(value) => appStore.setIpAddress(value)}
                  disabled={appStore.isConnected}
                  options={appStore.availableIpAddresses.map((ip) => ({ value: ip, label: ip }))}
                />
              </div>
              <Space>
                <Button
                  type="primary"
                  icon={<ApiOutlined />}
                  onClick={() => appStore.connect()}
                  loading={appStore.isLoading}
                  disabled={appStore.isConnected}
                >
                  Verbinden
                </Button>
                <Button
                  danger
                  icon={<DisconnectOutlined />}
                  onClick={() => appStore.disconnect()}
                  disabled={!appStore.isConnected}
                >
                  Trennen
                </Button>
              </Space>
              <Tag color={appStore.isConnected ? 'success' : 'error'}>
                {appStore.statusText}
              </Tag>
            </Space>
          </Card>
        </Col>

        {/* System State Widget - matches SystemStateWidget.razor */}
        <Col xs={24} md={8}>
          <Card title="System Status" size="small">
            <Row gutter={[8, 8]}>
              <Col span={12}>
                <Statistic
                  title="Main"
                  value={appStore.systemState.mainCurrent}
                  suffix="mA"
                  valueStyle={{ 
                    fontSize: 18,
                    color: appStore.systemState.mainCurrent > 2000 ? '#ff4d4f' : undefined 
                  }}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="Temp"
                  value={appStore.systemState.temperature}
                  suffix="°C"
                  valueStyle={{ 
                    fontSize: 18,
                    color: appStore.systemState.temperature > 60 ? '#ff4d4f' : undefined 
                  }}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="Supply"
                  value={appStore.supplyVoltageFormatted}
                  suffix="V"
                  valueStyle={{ fontSize: 18 }}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="VCC"
                  value={appStore.vccVoltageFormatted}
                  suffix="V"
                  valueStyle={{ fontSize: 18 }}
                />
              </Col>
            </Row>
            <div style={{ marginTop: 12, paddingTop: 12, borderTop: '1px solid #f0f0f0', fontSize: 12 }}>
              <div><strong>Seriennummer:</strong> {appStore.versionInfo.serialNumber}</div>
              <div><strong>Firmware:</strong> {appStore.versionInfo.firmwareVersion}</div>
              <div><strong>Hardware:</strong> {appStore.versionInfo.hardwareType}</div>
            </div>
          </Card>
        </Col>

        {/* Lap Counter Settings - matches lap-counter-widget in Blazor */}
        <Col xs={24} md={8}>
          <Card title="Lap Counter Settings" size="small">
            <Space direction="vertical" style={{ width: '100%' }}>
              <div>
                <label style={{ fontSize: 12, color: '#666' }}>Target Lap Count:</label>
                <div style={{ fontSize: 16, fontWeight: 500 }}>
                  {appStore.lapCounterSettings.globalTargetLapCount}
                </div>
              </div>
              <div>
                <label style={{ fontSize: 12, color: '#666' }}>Timer Filter:</label>
                <div>
                  <Tag color={appStore.lapCounterSettings.useTimerFilter ? 'blue' : 'default'}>
                    {appStore.lapCounterSettings.useTimerFilter ? 'Enabled' : 'Disabled'}
                  </Tag>
                  <span style={{ marginLeft: 8, fontSize: 12 }}>
                    ({appStore.lapCounterSettings.timerIntervalSeconds} s)
                  </span>
                </div>
              </div>
              <div>
                <label style={{ fontSize: 12, color: '#666' }}>Feedback Points:</label>
                <div>{appStore.lapCounterSettings.countOfFeedbackPoints}</div>
              </div>
            </Space>
          </Card>
        </Col>
      </Row>

      {/* Track Power Controls */}
      <Card size="small" style={{ marginBottom: 24 }}>
        <Space>
          <Button
            type={appStore.systemState.isTrackPowerOn ? 'primary' : 'default'}
            icon={<ThunderboltOutlined />}
            onClick={() => appStore.toggleTrackPower()}
            disabled={!appStore.isConnected}
          >
            {appStore.systemState.isTrackPowerOn ? 'Gleisspannung AUS' : 'Gleisspannung EIN'}
          </Button>
          <Button
            danger
            icon={<WarningOutlined />}
            onClick={() => appStore.emergencyStop()}
            disabled={!appStore.isConnected}
          >
            NOTHALT
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => appStore.resetCounters()}
            disabled={!appStore.isConnected}
          >
            Counter zurücksetzen
          </Button>
        </Space>
      </Card>

      {/* ════════════════════════════════════════════════════════════════════
          TRACK STATISTICS (matches tracks-section in Blazor)
          ════════════════════════════════════════════════════════════════════ */}
      <div className="tracks-section">
        <h3 style={{ marginBottom: 16 }}>Track Statistics</h3>

        {appStore.statistics.length === 0 ? (
          <Card>
            <Empty
              description={
                <span>
                  No track statistics configured.<br />
                  Set CountOfFeedbackPoints in settings.<br />
                  <small>Current value: {appStore.lapCounterSettings.countOfFeedbackPoints} feedback points</small>
                </span>
              }
            />
          </Card>
        ) : (
          <Row gutter={[16, 16]}>
            {appStore.statistics.map((stat) => (
              <Col xs={24} sm={12} lg={8} xl={6} key={stat.inPort}>
                <TrackCard stat={stat} />
              </Col>
            ))}
          </Row>
        )}
      </div>

      {/* Technology Comparison Info */}
      <Card title="ℹ️ Technologie-Vergleich" style={{ marginTop: 24 }} size="small">
        <Row gutter={16}>
          <Col span={12}>
            <h4>🔵 React (diese Seite)</h4>
            <ul style={{ paddingLeft: 20, margin: 0, fontSize: 12 }}>
              <li>MobX für State Management</li>
              <li>REST API Polling (2s Intervall)</li>
              <li>Ant Design Komponenten</li>
              <li>TypeScript</li>
            </ul>
          </Col>
          <Col span={12}>
            <h4>🟣 Blazor (zum Vergleich)</h4>
            <ul style={{ paddingLeft: 20, margin: 0, fontSize: 12 }}>
              <li>WebAppViewModel (C# ViewModel)</li>
              <li>SignalR Real-time Updates</li>
              <li>PropertyChanged Events</li>
              <li><a href="/blazor">Blazor Dashboard öffnen →</a></li>
            </ul>
          </Col>
        </Row>
      </Card>
    </div>
  )
})

/**
 * Track Statistics Card - matches track-card in Blazor Dashboard.razor
 */
const TrackCard = observer(({ stat }: { stat: TrackStatistic }) => {
  return (
    <Card
      size="small"
      title={
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Track {stat.inPort}</span>
          <Tag color={stat.hasReceivedFirstLap ? 'processing' : 'default'}>
            {stat.lapCountFormatted}
          </Tag>
        </div>
      }
      style={{
        borderTop: `3px solid ${stat.hasReceivedFirstLap ? '#52c41a' : '#d9d9d9'}`,
      }}
    >
      <Progress
        percent={Math.round(stat.progress * 100)}
        status={stat.progress >= 1 ? 'success' : 'active'}
        size="small"
        style={{ marginBottom: 12 }}
      />
      <div style={{ fontSize: 12 }}>
        <Row>
          <Col span={12}><strong>InPort:</strong></Col>
          <Col span={12}>{stat.inPort}</Col>
        </Row>
        <Row>
          <Col span={12}><strong>Lap Count:</strong></Col>
          <Col span={12}>{stat.count}</Col>
        </Row>
        <Row>
          <Col span={12}><strong>Target:</strong></Col>
          <Col span={12}>{stat.targetLapCount}</Col>
        </Row>
        {stat.lastFeedbackTime && stat.lastFeedbackTime !== '--:--:--' && (
          <Row>
            <Col span={12}><strong>Last Detected:</strong></Col>
            <Col span={12}>{stat.lastFeedbackTime}</Col>
          </Row>
        )}
        {stat.lastLapTime && stat.lastLapTime !== '--:--' && (
          <Row>
            <Col span={12}><strong>Last Lap Time:</strong></Col>
            <Col span={12}>{stat.lastLapTime}</Col>
          </Row>
        )}
      </div>
    </Card>
  )
})

export default DashboardPage

import { Card, Form, Input, Button, Switch, Divider, Space, message } from 'antd'
import { SaveOutlined, ReloadOutlined } from '@ant-design/icons'
import { observer } from 'mobx-react-lite'
import { useStore } from '../stores/StoreContext'

const SettingsPage = observer(() => {
  const { appStore } = useStore()
  const [messageApi, contextHolder] = message.useMessage()

  const handleSave = () => {
    messageApi.success('Einstellungen gespeichert')
  }

  return (
    <div>
      {contextHolder}
      <h1 style={{ marginBottom: 24 }}>⚙️ Einstellungen</h1>

      <Card title="Z21 Verbindung" style={{ marginBottom: 24 }}>
        <Form layout="vertical">
          <Form.Item label="IP-Adresse">
            <Input
              value={appStore.ipAddress}
              onChange={(e) => appStore.setIpAddress(e.target.value)}
              placeholder="192.168.0.111"
            />
          </Form.Item>
          <Form.Item label="Port">
            <Input defaultValue="21105" disabled />
          </Form.Item>
          <Form.Item label="Automatisch verbinden">
            <Switch defaultChecked={false} />
          </Form.Item>
        </Form>
      </Card>

      <Card title="Anwendung" style={{ marginBottom: 24 }}>
        <Form layout="vertical">
          <Form.Item label="Sprache">
            <Input defaultValue="Deutsch" disabled />
          </Form.Item>
          <Form.Item label="Theme">
            <Input defaultValue="Hell" disabled />
          </Form.Item>
          <Form.Item label="Debug-Modus">
            <Switch defaultChecked={false} />
          </Form.Item>
        </Form>
      </Card>

      <Card title="Technologie-Vergleich">
        <p>Diese Anwendung ist ein Validierungsprojekt zum Vergleich von:</p>
        <Divider />
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          <div>
            <h4>🔵 React (aktuell)</h4>
            <ul>
              <li>React 19 mit TypeScript</li>
              <li>MobX für State Management</li>
              <li>Ant Design für UI-Komponenten</li>
              <li>Vite für schnelle Entwicklung</li>
            </ul>
          </div>
          <div>
            <h4>🟣 Blazor (Vergleich)</h4>
            <ul>
              <li>Blazor Server mit .NET 10</li>
              <li>C# im Frontend</li>
              <li>SignalR für Echtzeit-Updates</li>
              <li>
                <a href="/blazor">Blazor-Version öffnen →</a>
              </li>
            </ul>
          </div>
        </Space>
      </Card>

      <div style={{ marginTop: 24, display: 'flex', gap: 8 }}>
        <Button type="primary" icon={<SaveOutlined />} onClick={handleSave}>
          Speichern
        </Button>
        <Button icon={<ReloadOutlined />}>Zurücksetzen</Button>
      </div>
    </div>
  )
})

export default SettingsPage

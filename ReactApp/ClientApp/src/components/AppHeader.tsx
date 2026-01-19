import { Layout, Space, Button, Tag } from 'antd'
import {
  ApiOutlined,
  DisconnectOutlined,
  GithubOutlined,
  SettingOutlined,
} from '@ant-design/icons'
import { observer } from 'mobx-react-lite'
import { useStore } from '../stores/StoreContext'

const { Header } = Layout

const AppHeader = observer(() => {
  const { appStore } = useStore()

  return (
    <Header className="app-header">
      <div className="logo">
        <span style={{ fontSize: '1.5rem' }}>🚂</span>
        <h1>MOBAflow</h1>
        <Tag color="blue">React + MobX</Tag>
      </div>
      <Space className="header-actions">
        <div className={`status-badge ${appStore.isConnected ? 'connected' : 'disconnected'}`}>
          {appStore.isConnected ? (
            <>
              <ApiOutlined /> Verbunden
            </>
          ) : (
            <>
              <DisconnectOutlined /> Nicht verbunden
            </>
          )}
        </div>
        <Button
          type="text"
          icon={<SettingOutlined />}
          style={{ color: 'white' }}
          href="/settings"
        />
        <Button
          type="text"
          icon={<GithubOutlined />}
          style={{ color: 'white' }}
          href="/blazor"
          title="Blazor Validation"
        />
      </Space>
    </Header>
  )
})

export default AppHeader

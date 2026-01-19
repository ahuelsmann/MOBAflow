import { useEffect } from 'react'
import { Table, Card, Button, Space, Tag, Empty, Progress } from 'antd'
import { PlayCircleOutlined, StopOutlined, ReloadOutlined } from '@ant-design/icons'
import { observer } from 'mobx-react-lite'
import { useStore } from '../stores/StoreContext'
import type { Journey } from '../stores/AppStore'
import type { ColumnsType } from 'antd/es/table'

const JourneysPage = observer(() => {
  const { appStore } = useStore()

  useEffect(() => {
    appStore.fetchJourneys()
  }, [appStore])

  const columns: ColumnsType<Journey> = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <strong>{text}</strong>,
    },
    {
      title: 'Zug',
      dataIndex: 'trainId',
      key: 'trainId',
      render: (trainId: number) => {
        const train = appStore.trains.find((t) => t.id === trainId)
        return train ? <Tag color="blue">{train.name}</Tag> : <Tag>Kein Zug</Tag>
      },
    },
    {
      title: 'Stationen',
      dataIndex: 'stations',
      key: 'stations',
      render: (stations: string[]) => (
        <Space wrap>
          {stations.map((station, index) => (
            <Tag key={index}>{station}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: 'Fortschritt',
      key: 'progress',
      width: 200,
      render: (_: unknown, record: Journey) => (
        <Progress
          percent={record.isRunning ? 50 : 0}
          status={record.isRunning ? 'active' : 'normal'}
          size="small"
        />
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isRunning',
      key: 'isRunning',
      render: (isRunning: boolean) => (
        <Tag color={isRunning ? 'processing' : 'default'}>
          {isRunning ? 'Läuft' : 'Gestoppt'}
        </Tag>
      ),
    },
    {
      title: 'Aktionen',
      key: 'actions',
      render: (_: unknown, record: Journey) => (
        <Space>
          <Button
            type={record.isRunning ? 'default' : 'primary'}
            danger={record.isRunning}
            icon={record.isRunning ? <StopOutlined /> : <PlayCircleOutlined />}
            disabled={!appStore.isConnected}
          >
            {record.isRunning ? 'Stopp' : 'Start'}
          </Button>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1>🛤️ Fahrten</h1>
        <Button icon={<ReloadOutlined />} onClick={() => appStore.fetchJourneys()} loading={appStore.isLoading}>
          Aktualisieren
        </Button>
      </div>

      <Card>
        {appStore.journeys.length === 0 ? (
          <Empty description="Keine Fahrten konfiguriert" />
        ) : (
          <Table
            columns={columns}
            dataSource={appStore.journeys}
            rowKey="id"
            loading={appStore.isLoading}
            pagination={false}
          />
        )}
      </Card>
    </div>
  )
})

export default JourneysPage

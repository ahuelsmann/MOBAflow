import { useEffect } from 'react'
import { Table, Card, Button, Space, Slider, Tag, Empty } from 'antd'
import { PlayCircleOutlined, PauseCircleOutlined, ReloadOutlined } from '@ant-design/icons'
import { observer } from 'mobx-react-lite'
import { useStore } from '../stores/StoreContext'
import type { Train } from '../stores/AppStore'
import type { ColumnsType } from 'antd/es/table'

const TrainsPage = observer(() => {
  const { appStore } = useStore()

  useEffect(() => {
    appStore.fetchTrains()
  }, [appStore])

  const columns: ColumnsType<Train> = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <strong>{text}</strong>,
    },
    {
      title: 'Adresse',
      dataIndex: 'address',
      key: 'address',
      render: (address: number) => <Tag color="blue">{address}</Tag>,
    },
    {
      title: 'Geschwindigkeit',
      dataIndex: 'speed',
      key: 'speed',
      width: 300,
      render: (speed: number, record: Train) => (
        <Space style={{ width: '100%' }}>
          <Slider
            style={{ width: 200 }}
            min={0}
            max={126}
            value={speed}
            onChange={(value) => appStore.setTrainSpeed(record.id, value)}
            disabled={!appStore.isConnected}
          />
          <span style={{ minWidth: 40 }}>{speed}</span>
        </Space>
      ),
    },
    {
      title: 'Richtung',
      dataIndex: 'direction',
      key: 'direction',
      render: (direction: string) => (
        <Tag color={direction === 'forward' ? 'green' : 'orange'}>
          {direction === 'forward' ? 'Vorwärts' : 'Rückwärts'}
        </Tag>
      ),
    },
    {
      title: 'Aktionen',
      key: 'actions',
      render: (_: unknown, record: Train) => (
        <Space>
          <Button
            type="primary"
            icon={record.speed > 0 ? <PauseCircleOutlined /> : <PlayCircleOutlined />}
            onClick={() => appStore.setTrainSpeed(record.id, record.speed > 0 ? 0 : 50)}
            disabled={!appStore.isConnected}
          >
            {record.speed > 0 ? 'Stopp' : 'Start'}
          </Button>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1>🚂 Züge</h1>
        <Button icon={<ReloadOutlined />} onClick={() => appStore.fetchTrains()} loading={appStore.isLoading}>
          Aktualisieren
        </Button>
      </div>

      <Card>
        {appStore.trains.length === 0 ? (
          <Empty description="Keine Züge konfiguriert" />
        ) : (
          <Table
            columns={columns}
            dataSource={appStore.trains}
            rowKey="id"
            loading={appStore.isLoading}
            pagination={false}
          />
        )}
      </Card>
    </div>
  )
})

export default TrainsPage

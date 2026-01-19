import { useState } from 'react'
import { Layout, Menu } from 'antd'
import { useNavigate, useLocation } from 'react-router-dom'
import {
  DashboardOutlined,
  CarOutlined,
  NodeIndexOutlined,
  SettingOutlined,
  SwapOutlined,
} from '@ant-design/icons'
import type { MenuProps } from 'antd'

const { Sider } = Layout

type MenuItem = Required<MenuProps>['items'][number]

const menuItems: MenuItem[] = [
  {
    key: '/dashboard',
    icon: <DashboardOutlined />,
    label: 'Dashboard',
  },
  {
    key: '/trains',
    icon: <CarOutlined />,
    label: 'Züge',
  },
  {
    key: '/journeys',
    icon: <NodeIndexOutlined />,
    label: 'Fahrten',
  },
  {
    type: 'divider',
  },
  {
    key: '/settings',
    icon: <SettingOutlined />,
    label: 'Einstellungen',
  },
  {
    key: '/blazor',
    icon: <SwapOutlined />,
    label: 'Blazor Validation',
  },
]

const AppSidebar = () => {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()

  const handleMenuClick: MenuProps['onClick'] = (e) => {
    if (e.key === '/blazor') {
      window.location.href = '/blazor'
    } else {
      navigate(e.key)
    }
  }

  return (
    <Sider
      collapsible
      collapsed={collapsed}
      onCollapse={(value) => setCollapsed(value)}
      style={{ background: '#fff' }}
    >
      <Menu
        mode="inline"
        selectedKeys={[location.pathname]}
        items={menuItems}
        onClick={handleMenuClick}
        style={{ height: '100%', borderRight: 0 }}
      />
    </Sider>
  )
}

export default AppSidebar

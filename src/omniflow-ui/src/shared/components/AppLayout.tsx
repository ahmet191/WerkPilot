import React, { useEffect, useState } from 'react';
import { Layout, Menu, Select, Space, message, Button, Spin } from 'antd';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import * as Icons from '@ant-design/icons';
import { useAppTheme } from '../../core/ThemeContext';
import type { AppTheme } from '../../core/ThemeContext';
import { useTranslation } from '../../core/LanguageContext';

const { Header, Sider, Content } = Layout;

export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { theme, setTheme } = useAppTheme();
  const { language, setLanguage, t } = useTranslation();
  
  const [collapsed, setCollapsed] = useState(() => {
    return localStorage.getItem('sidebar_collapsed') === 'true';
  });
  
  const [menuData, setMenuData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    localStorage.setItem('sidebar_collapsed', collapsed.toString());
  }, [collapsed]);

  useEffect(() => {
    fetch('http://localhost:5226/api/menu')
      .then(res => res.json())
      .then(data => {
        setMenuData(data);
        setLoading(false);
      })
      .catch(err => {
        message.error('Failed to load menus');
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    const handleApiError = (event: any) => {
      const messageKey = event.detail?.messageKey;
      if (messageKey) {
        message.error(t(messageKey, 'An error occurred'));
      }
    };
    window.addEventListener('api-error', handleApiError);
    return () => window.removeEventListener('api-error', handleApiError);
  }, [t]);

  const renderIcon = (iconName?: string) => {
    if (!iconName) return null;
    const IconComponent = (Icons as any)[iconName];
    return IconComponent ? <IconComponent /> : <Icons.AppstoreOutlined />;
  };

  const generateMenuProps = (items: any[]): any[] => {
    return items.map(item => {
      if (item.children && item.children.length > 0) {
        return {
          key: item.id,
          icon: renderIcon(item.icon),
          label: t(item.titleKey) || item.titleKey,
          children: generateMenuProps(item.children)
        };
      }
      return {
        key: item.path || item.id,
        icon: renderIcon(item.icon),
        label: t(item.titleKey) || item.titleKey,
      };
    });
  };

  const handleMenuClick = ({ key }: any) => {
    // If key looks like a path, navigate
    if (key.startsWith('/')) {
      navigate(key);
    }
  };

  return (
    <Layout className="app-layout" style={{ minHeight: '100vh' }}>
      <Sider 
        width={250} 
        className="sidebar"
        collapsible 
        collapsed={collapsed} 
        onCollapse={(value) => setCollapsed(value)}
        theme={theme === 'light' ? 'light' : 'dark'}
      >
        <div style={{ 
          height: 64, 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          fontWeight: 'bold', 
          fontSize: collapsed ? 24 : 20, 
          color: 'var(--accent-color)', 
          borderBottom: '1px solid var(--border-color)',
          transition: 'all 0.2s'
        }}>
          {collapsed ? <Icons.ApiOutlined /> : 'OmniFlow ESM'}
        </div>
        
        {loading ? (
          <div style={{ textAlign: 'center', padding: '20px' }}><Spin /></div>
        ) : (
          <Menu
            mode="inline"
            selectedKeys={[location.pathname]}
            items={generateMenuProps(menuData)}
            onClick={handleMenuClick}
            style={{ borderRight: 0, background: 'transparent' }}
            theme={theme === 'light' ? 'light' : 'dark'}
          />
        )}
      </Sider>
      
      <Layout className="main-content">
        <Header className="top-header" style={{ background: 'var(--bg-panel)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0 24px' }}>
          <div>
            {React.createElement(collapsed ? Icons.MenuUnfoldOutlined : Icons.MenuFoldOutlined, {
              className: 'trigger',
              onClick: () => setCollapsed(!collapsed),
              style: { fontSize: '18px', cursor: 'pointer' }
            })}
          </div>
          <Space size="middle">
            {/* Theme Switcher */}
            <Space size="small" style={{ fontSize: 12 }}>
              <Icons.BgColorsOutlined style={{ color: 'var(--text-secondary)' }} />
              <Select
                size="small"
                value={theme}
                onChange={(val) => setTheme(val as AppTheme)}
                style={{ width: 110 }}
                options={[
                  { value: 'light', label: 'Light' },
                  { value: 'dark', label: 'Dark' },
                  { value: 'slate-blue', label: 'Steel Blue' },
                ]}
              />
            </Space>

            {/* Language Switcher */}
            <Space size="small" style={{ fontSize: 12 }}>
              <Icons.TranslationOutlined style={{ color: 'var(--text-secondary)' }} />
              <Select
                size="small"
                value={language}
                onChange={setLanguage}
                style={{ width: 90 }}
                options={[
                  { value: 'en-US', label: 'English' },
                  { value: 'tr-TR', label: 'Türkçe' },
                ]}
              />
            </Space>
          </Space>
        </Header>
        <Content className="page-content" style={{ margin: '24px', background: 'var(--bg-panel)', borderRadius: '8px', overflow: 'hidden' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}

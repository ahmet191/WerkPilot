import React, { useEffect } from 'react';
import { Layout, Menu, Select, Space, message } from 'antd';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { TeamOutlined, LaptopOutlined, ContainerOutlined, TranslationOutlined, BgColorsOutlined, DashboardOutlined, CalendarOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import { useAppTheme } from '../../core/ThemeContext';
import type { AppTheme } from '../../core/ThemeContext';
import { useTranslation } from '../../core/LanguageContext';

const { Header, Sider, Content } = Layout;

export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { theme, setTheme } = useAppTheme();
  const { language, setLanguage, t } = useTranslation();

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

  const menuItems = [
    { key: '/', icon: <DashboardOutlined />, label: t('sidebar.dashboard') || 'Dashboard' },
    { key: '/departments', icon: <TeamOutlined />, label: t('sidebar.departments') },
    { key: '/assets', icon: <LaptopOutlined />, label: t('sidebar.assets') },
    { key: '/tickets', icon: <ContainerOutlined />, label: t('sidebar.tickets') },
    { key: '/leaves', icon: <CalendarOutlined />, label: t('sidebar.leaves') || 'Leaves' },
    { key: '/auditlogs', icon: <SafetyCertificateOutlined />, label: t('sidebar.auditlogs') || 'Audit Logs' },
  ];

  return (
    <Layout className="app-layout">
      <Sider width={200} className="sidebar">
        <div style={{ height: 48, display: 'flex', alignItems: 'center', padding: '0 16px', fontWeight: 'bold', fontSize: 16, color: 'var(--accent-color)', borderBottom: '1px solid var(--border-color)' }}>
          OmniFlow ESM
        </div>
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ borderRight: 0, background: 'transparent' }}
          theme={theme === 'light' ? 'light' : 'dark'}
        />
      </Sider>
      <Layout className="main-content">
        <Header className="top-header" style={{ background: 'var(--bg-panel)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div />
          <Space size="middle">
            {/* Theme Switcher */}
            <Space size="small" style={{ fontSize: 12 }}>
              <BgColorsOutlined style={{ color: 'var(--text-secondary)' }} />
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
              <TranslationOutlined style={{ color: 'var(--text-secondary)' }} />
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
        <Content className="page-content">
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}

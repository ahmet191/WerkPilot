import React, { useEffect, useState } from 'react';
import { Layout, Menu, Select, Space, message, Spin, Dropdown, Avatar, Tag, Divider } from 'antd';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  SafetyCertificateOutlined, TeamOutlined, TranslationOutlined, CloudServerOutlined,
  LogoutOutlined, MenuUnfoldOutlined, MenuFoldOutlined, BgColorsOutlined, GlobalOutlined,
  DownOutlined, LayoutOutlined,
} from '@ant-design/icons';
import { resolveIcon } from '../menuIcons';
import { useAppTheme } from '../../core/ThemeContext';
import type { AppTheme } from '../../core/ThemeContext';
import { useTranslation } from '../../core/LanguageContext';
import { useAuth } from '../../core/AuthContext';
import { usePermissions, Perm } from '../../core/PermissionContext';
import { api } from '../../core/api';
import { NotificationBell } from './NotificationBell';

const { Header, Sider, Content } = Layout;

export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { theme, setTheme } = useAppTheme();
  const { language, setLanguage, t } = useTranslation();
  const { user, logout } = useAuth();
  const { has } = usePermissions();

  const [collapsed, setCollapsed] = useState(() => {
    return localStorage.getItem('sidebar_collapsed') === 'true';
  });

  const [menuData, setMenuData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    localStorage.setItem('sidebar_collapsed', collapsed.toString());
  }, [collapsed]);

  useEffect(() => {
    api.get('/menu')
      .then(res => {
        setMenuData(res.data);
        setLoading(false);
      })
      .catch(() => {
        message.error('Failed to load menus');
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    const handleApiError = (event: any) => {
      const { messageKey, traceId, errors, detail } = event.detail ?? {};
      if (!messageKey) return;

      // Prefer field-level validation errors, then the ProblemDetails detail,
      // then the translated message key. Show the traceId for supportability.
      const firstFieldError = errors ? (Object.values(errors).flat()[0] as string) : undefined;
      const text = firstFieldError ?? detail ?? t(messageKey, 'An error occurred');
      message.error(
        <span>
          {text}
          {traceId && (
            <span style={{ display: 'block', fontSize: 11, opacity: 0.65 }}>
              Trace: {traceId}
            </span>
          )}
        </span>,
      );
    };
    window.addEventListener('api-error', handleApiError);
    return () => window.removeEventListener('api-error', handleApiError);
  }, [t]);

  const renderIcon = (iconName?: string) => resolveIcon(iconName);

  // Admin-only destinations — mirrors the ADMIN_ROLES route guards in App.tsx.
  // Hidden from the sidebar for non-admins so they never see a link that 403s.
  const ADMIN_PATHS = new Set([
    '/servicedesks', '/departments', '/auditlogs', '/templates',
    '/categories', '/slas', '/users', '/menus',
  ]);
  const isAdmin = (user?.roles ?? []).some(r => r === 'Administrator' || r === 'HRAdmin');

  // Drop admin leaves for non-admins, then drop parents left with no children.
  const filterMenuByRole = (items: any[]): any[] => {
    if (isAdmin) return items;
    return items.reduce<any[]>((acc, item) => {
      if (item.children && item.children.length > 0) {
        const children = filterMenuByRole(item.children);
        if (children.length > 0) acc.push({ ...item, children });
      } else if (!ADMIN_PATHS.has(item.path)) {
        acc.push(item);
      }
      return acc;
    }, []);
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
    if (key.startsWith('/')) {
      navigate(key);
    }
  };

  const displayName = user?.name && user.name !== user.email ? user.name : (user?.email?.split('@')[0] ?? 'User');
  const initials = displayName.slice(0, 2).toUpperCase();
  const primaryRole = (user?.roles ?? [])[0];

  const userMenuItems = [
    {
      key: 'identity',
      disabled: true,
      label: (
        <div style={{ padding: '4px 4px 6px', minWidth: 200, cursor: 'default' }}>
          <div style={{ fontWeight: 650, fontSize: 13, color: 'var(--text-primary)' }}>{displayName}</div>
          <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 6 }}>{user?.email}</div>
          <Space size={4} wrap>
            {(user?.roles ?? []).map(role => (
              <Tag key={role} style={{ fontSize: 10, lineHeight: '16px', margin: 0 }}>{role}</Tag>
            ))}
          </Space>
        </div>
      ),
    },
    { type: 'divider' as const },
    {
      key: 'hrCenter',
      icon: <TeamOutlined />,
      label: t('sidebar.hrCenter', 'My HR Center (Özlük)'),
      onClick: () => navigate('/hr/center'),
    },
    {
      key: 'security',
      icon: <SafetyCertificateOutlined />,
      label: t('security.title', 'Security'),
      onClick: () => navigate('/settings/security'),
    },
    ...(has(Perm.PermissionsManage) ? [{
      key: 'permissions',
      icon: <TeamOutlined />,
      label: t('permissions.title', 'Roles & Permissions'),
      onClick: () => navigate('/settings/permissions'),
    }] : []),
    ...(has(Perm.TranslationsManage) ? [{
      key: 'translations',
      icon: <TranslationOutlined />,
      label: t('translations.title', 'Interface text'),
      onClick: () => navigate('/settings/translations'),
    }] : []),
    ...(has(Perm.UiSchemaManage) ? [{
      key: 'layouts',
      icon: <LayoutOutlined />,
      label: t('layouts.title', 'Screen layouts'),
      onClick: () => navigate('/settings/layouts'),
    }] : []),
    ...((user?.roles ?? []).includes('PlatformAdmin') ? [{
      key: 'platform',
      icon: <CloudServerOutlined />,
      label: t('platform.title', 'Workspaces'),
      onClick: () => navigate('/platform/tenants'),
    }] : []),
    { type: 'divider' as const },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      danger: true,
      label: t('auth.signout', 'Sign out'),
      onClick: logout,
    },
  ];

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
        {/* Brand block */}
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'flex-start',
            gap: 10,
            padding: collapsed ? 0 : '0 18px',
            borderBottom: '1px solid var(--border-color)',
            transition: 'all 0.2s',
          }}
        >
          <span className="brand-mark" style={{ width: 30, height: 30, fontSize: 15 }}>W</span>
          {!collapsed && (
            <span style={{ lineHeight: 1.15 }}>
              <span style={{ display: 'block', fontWeight: 750, fontSize: 16, letterSpacing: 0.2, color: 'var(--text-primary)' }}>
                WerkPilot
              </span>
              <span style={{ display: 'block', fontSize: 10, letterSpacing: 1.6, textTransform: 'uppercase', color: 'var(--text-tertiary)' }}>
                ESM Platform
              </span>
            </span>
          )}
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: '20px' }}><Spin /></div>
        ) : (
          <Menu
            mode="inline"
            selectedKeys={[location.pathname]}
            items={generateMenuProps(filterMenuByRole(menuData))}
            onClick={handleMenuClick}
            style={{ borderRight: 0, background: 'transparent' }}
            theme={theme === 'light' ? 'light' : 'dark'}
          />
        )}
      </Sider>

      <Layout className="main-content">
        <Header
          className="top-header"
          style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0 20px', background: 'transparent' }}
        >
          <div>
            {React.createElement(collapsed ? MenuUnfoldOutlined : MenuFoldOutlined, {
              className: 'trigger',
              onClick: () => setCollapsed(!collapsed),
              style: { fontSize: '17px', cursor: 'pointer', color: 'var(--text-secondary)' }
            })}
          </div>
          <Space size="middle" align="center">
            {/* Theme Switcher */}
            <Space size={6} style={{ fontSize: 12 }}>
              <BgColorsOutlined style={{ color: 'var(--text-tertiary)' }} />
              <Select
                size="small"
                variant="borderless"
                value={theme}
                onChange={(val) => setTheme(val as AppTheme)}
                style={{ width: 108 }}
                options={[
                  { value: 'light', label: 'Light' },
                  { value: 'dark', label: 'Dark' },
                  { value: 'slate-blue', label: 'Steel Blue' },
                ]}
              />
            </Space>

            {/* Language Switcher */}
            <Space size={6} style={{ fontSize: 12 }}>
              <GlobalOutlined style={{ color: 'var(--text-tertiary)' }} />
              <Select
                size="small"
                variant="borderless"
                value={language}
                onChange={setLanguage}
                style={{ width: 110 }}
                options={[
                  { value: 'en-US', label: 'English 🇬🇧' },
                  { value: 'tr-TR', label: 'Türkçe 🇹🇷' },
                  { value: 'de-DE', label: 'Deutsch 🇩🇪' },
                ]}
              />
            </Space>

            <NotificationBell />

            <Divider type="vertical" style={{ height: 22, margin: 0 }} />

            {/* User chip → dropdown */}
            <Dropdown menu={{ items: userMenuItems }} trigger={['click']} placement="bottomRight">
              <span className="user-chip">
                <Avatar size={28} style={{ background: 'var(--accent-gradient)', fontWeight: 650, fontSize: 12 }}>
                  {initials}
                </Avatar>
                <span style={{ lineHeight: 1.1, textAlign: 'left' }}>
                  <span style={{ display: 'block', fontSize: 12.5, fontWeight: 600, color: 'var(--text-primary)' }}>
                    {displayName}
                  </span>
                  {primaryRole && (
                    <span style={{ display: 'block', fontSize: 10.5, color: 'var(--text-tertiary)' }}>
                      {primaryRole}
                    </span>
                  )}
                </span>
                <DownOutlined style={{ fontSize: 9, color: 'var(--text-tertiary)' }} />
              </span>
            </Dropdown>
          </Space>
        </Header>

        {/* Transparent shell: pages bring their own cards. The key re-triggers
            the fade-in on every route change. */}
        <Content className="page-content">
          <div key={location.pathname} className="page-fade-in">
            <Outlet />
          </div>
        </Content>
      </Layout>
    </Layout>
  );
}

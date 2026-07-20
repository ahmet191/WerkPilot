import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Badge, Button, Dropdown, Empty, List, Space, Spin, Typography } from 'antd';
import * as Icons from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { api } from '../../core/api';
import { TOKEN_KEY } from '../../core/config';
import { useTranslation } from '../../core/LanguageContext';

const { Text } = Typography;

interface NotificationItem {
  id: string;
  kind: string;
  title: string;
  body: string;
  link?: string | null;
  createdAt: string;
  isRead: boolean;
}

const KIND_ICON: Record<string, React.ReactNode> = {
  ApprovalRequested: <Icons.AuditOutlined />,
  ApprovalDecided: <Icons.CheckCircleOutlined />,
  SlaBreached: <Icons.FireOutlined />,
  TicketCreated: <Icons.ContainerOutlined />,
  TicketAssigned: <Icons.UserOutlined />,
  TicketStateChanged: <Icons.SyncOutlined />,
  AssetAssigned: <Icons.LaptopOutlined />,
  LeaveDecision: <Icons.CalendarOutlined />,
};

/** Relative time without pulling in a date library. */
function timeAgo(iso: string, t: (key: string, fallback: string) => string): string {
  const seconds = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
  if (seconds < 60) return t('time.justNow', 'just now');
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h`;
  return `${Math.floor(hours / 24)}d`;
}

/** Header bell: unread badge, recent list, live arrival over SignalR. */
export const NotificationBell: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [unread, setUnread] = useState(0);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [listRes, countRes] = await Promise.all([
        api.get('/notifications?take=15'),
        api.get('/notifications/unread-count'),
      ]);
      setItems(listRes.data ?? []);
      setUnread(countRes.data?.count ?? 0);
    } catch {
      // toast handled globally
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  // Live delivery: a push updates the badge without waiting for a poll.
  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => localStorage.getItem(TOKEN_KEY) ?? '' })
      .withAutomaticReconnect()
      .build();

    connection.on('notification', (payload: NotificationItem) => {
      setItems(prev => [{ ...payload, isRead: false }, ...prev].slice(0, 15));
      setUnread(prev => prev + 1);
    });

    connection.start().catch(() => undefined);
    connectionRef.current = connection;

    return () => {
      connectionRef.current?.stop().catch(() => undefined);
      connectionRef.current = null;
    };
  }, []);

  const openNotification = async (item: NotificationItem) => {
    setOpen(false);
    if (!item.isRead) {
      setItems(prev => prev.map(n => (n.id === item.id ? { ...n, isRead: true } : n)));
      setUnread(prev => Math.max(0, prev - 1));
      api.post(`/notifications/${item.id}/read`).catch(() => undefined);
    }
    if (item.link) navigate(item.link);
  };

  const markAllRead = async () => {
    setItems(prev => prev.map(n => ({ ...n, isRead: true })));
    setUnread(0);
    await api.post('/notifications/read-all').catch(() => undefined);
  };

  const panel = (
    <div className="glass-panel" style={{ width: 380, borderRadius: 'var(--radius-md)', overflow: 'hidden' }}>
      <div className="grid-toolbar" style={{ padding: '10px 14px' }}>
        <Space>
          <Icons.BellOutlined style={{ color: 'var(--accent-color)' }} />
          <span className="grid-title">{t('notifications.title', 'Notifications')}</span>
        </Space>
        {unread > 0 && (
          <Button type="link" size="small" onClick={markAllRead}>
            {t('notifications.markAllRead', 'Mark all read')}
          </Button>
        )}
      </div>

      <div style={{ maxHeight: 400, overflowY: 'auto' }}>
        {loading && items.length === 0 ? (
          <div style={{ padding: 32, textAlign: 'center' }}><Spin /></div>
        ) : items.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={t('notifications.empty', "You're all caught up")}
            style={{ padding: '24px 0' }}
          />
        ) : (
          <List
            size="small"
            dataSource={items}
            renderItem={item => (
              <List.Item
                onClick={() => openNotification(item)}
                style={{
                  cursor: 'pointer',
                  padding: '10px 14px',
                  background: item.isRead ? 'transparent' : 'var(--accent-soft)',
                }}
              >
                <List.Item.Meta
                  avatar={
                    <span className="stat-icon-chip" style={{ width: 30, height: 30, fontSize: 14 }}>
                      {KIND_ICON[item.kind] ?? <Icons.BellOutlined />}
                    </span>
                  }
                  title={
                    <Space size={6}>
                      <span style={{ fontWeight: item.isRead ? 400 : 600, fontSize: 13 }}>{item.title}</span>
                      <Text type="secondary" style={{ fontSize: 11 }}>{timeAgo(item.createdAt, t)}</Text>
                    </Space>
                  }
                  description={<span style={{ fontSize: 12 }}>{item.body}</span>}
                />
              </List.Item>
            )}
          />
        )}
      </div>
    </div>
  );

  return (
    <Dropdown
      open={open}
      onOpenChange={next => { setOpen(next); if (next) load(); }}
      trigger={['click']}
      placement="bottomRight"
      popupRender={() => panel}
    >
      <Badge count={unread} size="small" offset={[-2, 2]}>
        <Button type="text" icon={<Icons.BellOutlined style={{ fontSize: 17 }} />} />
      </Badge>
    </Dropdown>
  );
};

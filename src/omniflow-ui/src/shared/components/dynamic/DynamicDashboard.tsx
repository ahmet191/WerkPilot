import React, { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { Row, Col, Statistic, Skeleton } from 'antd';
import { ContainerOutlined, ExclamationCircleOutlined, FireOutlined, LaptopOutlined } from '@ant-design/icons';
import { SchemaService } from '../../../services/schema.service';
import type { UiSchema } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';
import { api } from '../../../core/api';
import { HUBS_URL, TOKEN_KEY } from '../../../core/config';

const IconMap: Record<string, React.ReactNode> = {
  ticket: <ContainerOutlined />,
  exclamation: <ExclamationCircleOutlined />,
  fire: <FireOutlined />,
  laptop: <LaptopOutlined />
};

export const DynamicDashboard: React.FC = () => {
  const [schema, setSchema] = useState<UiSchema | null>(null);
  const [data, setData] = useState<Record<string, any>>({});
  const [loading, setLoading] = useState(true);
  const { t } = useTranslation();
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    let disposed = false;

    const loadSchema = async () => {
      setLoading(true);
      try {
        const s = await SchemaService.getSchema('dashboard');
        if (disposed) return;
        setSchema(s);

        if (s.widgets && s.widgets.length > 0) {
          const response = await api.get(s.widgets[0].endpoint);
          if (!disposed) setData(response.data);
        }
      } catch (error) {
        console.error('Failed to load dashboard schema', error);
      } finally {
        if (!disposed) setLoading(false);
      }
    };

    const connectHub = async () => {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${HUBS_URL}/dashboard`, {
          // Supplies the JWT so the hub can require auth (Phase 5 backend follow-up).
          accessTokenFactory: () => localStorage.getItem(TOKEN_KEY) ?? '',
        })
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveMetricUpdate', (update: { widgetId: string; incrementValue: number }) => {
        setData(prev => ({
          ...prev,
          [update.widgetId]: (prev[update.widgetId] || 0) + update.incrementValue,
        }));
      });

      try {
        await connection.start();
        if (disposed) {
          // Unmounted mid-handshake — tear the socket down immediately.
          await connection.stop();
          return;
        }
        connectionRef.current = connection;
      } catch (err) {
        console.error('SignalR connection failed', err);
      }
    };

    loadSchema();
    connectHub();

    // Cleanup: stop the hub on unmount so we never leak a live socket.
    return () => {
      disposed = true;
      connectionRef.current?.stop();
      connectionRef.current = null;
    };
  }, []);

  if (loading || !schema) {
    // Skeleton stat cards: same geometry as the real ones, no layout shift.
    return (
      <div>
        <Skeleton.Input active size="small" style={{ width: 260, marginBottom: 20 }} />
        <Row gutter={[16, 16]}>
          {[0, 1, 2, 3].map(i => (
            <Col xs={24} sm={12} lg={6} key={i}>
              <div className="stat-card">
                <Skeleton active paragraph={{ rows: 1, width: '55%' }} title={{ width: '35%' }} />
              </div>
            </Col>
          ))}
        </Row>
      </div>
    );
  }

  const hour = new Date().getHours();
  const greetingKey = hour < 12 ? 'dashboard.goodMorning' : hour < 18 ? 'dashboard.goodAfternoon' : 'dashboard.goodEvening';
  const greetingFallback = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
  const today = new Date().toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' });

  return (
    <div>
      {/* Greeting header */}
      <div style={{ marginBottom: 20 }}>
        <h2 style={{ margin: 0, fontSize: 20, fontWeight: 700, letterSpacing: 0.1 }}>
          {t(greetingKey, greetingFallback)} 👋
        </h2>
        <span style={{ fontSize: 12.5, color: 'var(--text-tertiary)', textTransform: 'capitalize' }}>{today}</span>
      </div>

      <Row gutter={[16, 16]}>
        {schema.widgets?.map((widget, index) => (
          <Col xs={24} sm={12} lg={6} key={widget.dataKey}>
            <div
              className="stat-card card-lift"
              style={{ animation: `fadeInUp 0.3s ${0.05 * index}s cubic-bezier(0.4,0,0.2,1) both` }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                <span
                  className="stat-icon-chip"
                  style={{ background: `linear-gradient(135deg, ${widget.color}, ${widget.color}99)` }}
                >
                  {IconMap[widget.icon] || null}
                </span>
                <div>
                  <div style={{ fontSize: 12, fontWeight: 550, color: 'var(--text-secondary)', letterSpacing: 0.3 }}>
                    {t(widget.titleKey)}
                  </div>
                  <Statistic
                    value={data[widget.dataKey] || 0}
                    styles={{ content: { fontSize: 26, fontWeight: 700, lineHeight: 1.2, color: 'var(--text-primary)' } }}
                  />
                </div>
              </div>
            </div>
          </Col>
        ))}
      </Row>
    </div>
  );
};

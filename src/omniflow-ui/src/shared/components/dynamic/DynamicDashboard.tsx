import React, { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { Row, Col, Card, Statistic, Spin } from 'antd';
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
    return <Spin style={{ margin: '50px auto', display: 'block' }} />;
  }

  return (
    <div style={{ padding: 24 }}>
      <Row gutter={[16, 16]}>
        {schema.widgets?.map((widget) => (
          <Col xs={24} sm={12} md={6} key={widget.dataKey}>
            <Card variant="borderless">
              <Statistic
                title={t(widget.titleKey)}
                value={data[widget.dataKey] || 0}
                styles={{ content: { color: widget.color } }}
                prefix={IconMap[widget.icon] || null}
              />
            </Card>
          </Col>
        ))}
      </Row>
    </div>
  );
};

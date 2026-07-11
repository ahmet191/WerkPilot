import React, { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { Row, Col, Card, Statistic, Spin } from 'antd';
import { ContainerOutlined, ExclamationCircleOutlined, FireOutlined, LaptopOutlined } from '@ant-design/icons';
import { SchemaService } from '../../../services/schema.service';
import type { UiSchema, WidgetSchema } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';
import { api } from '../../../core/api';

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

  useEffect(() => {
    loadSchema();
  }, []);

  const loadSchema = async () => {
    setLoading(true);
    try {
      const s = await SchemaService.getSchema('dashboard');
      setSchema(s);
      
      if (s.widgets && s.widgets.length > 0) {
        const endpoint = s.widgets[0].endpoint;
        const response = await api.get(endpoint);
        setData(response.data);
      }

      // Initialize SignalR Connection
      const connection = new signalR.HubConnectionBuilder()
          .withUrl('http://localhost:5226/hubs/dashboard')
          .withAutomaticReconnect()
          .build();

      connection.on('ReceiveMetricUpdate', (update: { widgetId: string, incrementValue: number }) => {
          setData(prev => ({
              ...prev,
              [update.widgetId]: (prev[update.widgetId] || 0) + update.incrementValue
          }));
      });

      await connection.start();
      console.log('SignalR Connected to DashboardHub');

    } catch (error) {
      console.error('Failed to load dashboard schema', error);
    } finally {
      setLoading(false);
    }
  };

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

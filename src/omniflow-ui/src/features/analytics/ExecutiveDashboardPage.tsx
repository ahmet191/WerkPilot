import React, { useEffect, useState } from 'react';
import { Card, Row, Col, Statistic, Spin, Typography } from 'antd';
import { Area, Pie } from '@ant-design/plots';
import { BarChartOutlined, DashboardOutlined, FieldTimeOutlined, RobotOutlined, WarningOutlined } from '@ant-design/icons';
import { api } from '../../core/api';

const { Title, Text } = Typography;

interface DashboardMetrics {
  totalActiveTickets: number;
  averageResolutionTimeHours: number;
  firstContactResolutionPercentage: number;
  slaBreachRatePercentage: number;
  aiPredictionsCount: number;
}

export const ExecutiveDashboardPage: React.FC = () => {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchMetrics = async () => {
      try {
        const res = await api.get('/analytics/dashboard');
        setMetrics(res.data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchMetrics();
  }, []);

  // Mock data for charts to demonstrate executive view
  const trendData = [
    { date: '2026-07-01', count: 120 },
    { date: '2026-07-08', count: 150 },
    { date: '2026-07-15', count: 130 },
    { date: '2026-07-22', count: 180 },
  ];

  const deptData = [
    { type: 'IT', value: 45 },
    { type: 'HR', value: 25 },
    { type: 'Finance', value: 15 },
    { type: 'Legal', value: 10 },
    { type: 'Facilities', value: 5 },
  ];

  if (loading) {
    return <div style={{ textAlign: 'center', padding: 50 }}><Spin size="large" /></div>;
  }

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: 24 }}>
        <DashboardOutlined style={{ fontSize: 28, color: '#1890ff', marginRight: 12 }} />
        <div>
          <Title level={2} style={{ margin: 0 }}>Executive Analytics Dashboard</Title>
          <Text type="secondary">AI-Powered insights and service desk performance KPIs</Text>
        </div>
      </div>

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col span={6}>
          <Card hoverable style={{ borderLeft: '4px solid #1890ff' }}>
            <Statistic
              title="Active Tickets"
              value={metrics?.totalActiveTickets}
              prefix={<BarChartOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card hoverable style={{ borderLeft: '4px solid #52c41a' }}>
            <Statistic
              title="Avg Resolution (Hours)"
              value={metrics?.averageResolutionTimeHours}
              prefix={<FieldTimeOutlined />}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card hoverable style={{ borderLeft: '4px solid #faad14' }}>
            <Statistic
              title="FCR Rate (%)"
              value={metrics?.firstContactResolutionPercentage}
              precision={1}
              suffix="%"
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card hoverable style={{ borderLeft: '4px solid #f5222d' }}>
            <Statistic
              title="SLA Breach Rate"
              value={metrics?.slaBreachRatePercentage}
              precision={1}
              suffix="%"
              prefix={<WarningOutlined />}
              valueStyle={{ color: '#cf1322' }}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]}>
        <Col span={16}>
          <Card title="Ticket Volume Trend (30 Days)" style={{ height: '100%' }}>
            <Area
              data={trendData}
              xField="date"
              yField="count"
              height={300}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card title="Distribution by Department" style={{ height: '100%' }}>
            <Pie
              data={deptData}
              angleField="value"
              colorField="type"
              radius={0.8}
              innerRadius={0.6}
              label={{ type: 'inner', offset: '-50%', content: '{value}' }}
              height={300}
            />
          </Card>
        </Col>
      </Row>

      <Row style={{ marginTop: 24 }}>
        <Col span={24}>
          <Card>
            <div style={{ display: 'flex', alignItems: 'center' }}>
              <RobotOutlined style={{ fontSize: 32, color: '#722ed1', marginRight: 16 }} />
              <div>
                <Title level={4} style={{ margin: 0, color: '#722ed1' }}>AI Triaging Active</Title>
                <Text>
                  The Gemini AI model has successfully processed <strong>{metrics?.aiPredictionsCount}</strong> tickets, 
                  automatically predicting categories, priorities, and routing them to the correct service desks.
                </Text>
              </div>
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

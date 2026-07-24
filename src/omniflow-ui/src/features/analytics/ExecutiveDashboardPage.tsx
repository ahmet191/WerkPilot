import React, { useEffect, useState } from 'react';
import { Card, Row, Col, Statistic, Spin, Typography, theme, Empty } from 'antd';
import { Area, Pie } from '@ant-design/plots';
import { BarChartOutlined, DashboardOutlined, FieldTimeOutlined, RobotOutlined, WarningOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text } = Typography;

interface DashboardMetrics {
  totalActiveTickets: number;
  averageResolutionTimeHours: number;
  firstContactResolutionPercentage: number;
  slaBreachRatePercentage: number;
  aiPredictionsCount: number;
}

interface TrendPoint { date: string; count: number }
interface PriorityPoint { priority: string; count: number }

export const ExecutiveDashboardPage: React.FC = () => {
  const { t } = useTranslation();
  const { token } = theme.useToken();
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [trend, setTrend] = useState<TrendPoint[]>([]);
  const [byPriority, setByPriority] = useState<{ type: string; value: number }[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const [m, tr, pr] = await Promise.all([
          api.get<DashboardMetrics>('/analytics/dashboard'),
          api.get<TrendPoint[]>('/analytics/reports/volume-trend?days=30'),
          api.get<PriorityPoint[]>('/analytics/reports/tickets-by-priority'),
        ]);
        setMetrics(m.data);
        setTrend(tr.data ?? []);
        setByPriority((pr.data ?? []).map(p => ({ type: p.priority, value: p.count })));
      } catch {
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) {
    return <div style={{ textAlign: 'center', padding: 50 }}><Spin size="large" /></div>;
  }

  const trendHasData = trend.some(p => p.count > 0);
  const priorityHasData = byPriority.some(p => p.value > 0);

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: 24 }}>
        <DashboardOutlined style={{ fontSize: 28, color: token.colorPrimary, marginRight: 12 }} />
        <div>
          <Title level={2} style={{ margin: 0 }}>{t('execDash.title', 'Executive Analytics Dashboard')}</Title>
          <Text type="secondary">{t('execDash.subtitle', 'AI-assisted insights and service desk performance KPIs')}</Text>
        </div>
      </div>

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable style={{ borderLeft: `4px solid ${token.colorPrimary}` }}>
            <Statistic title={t('execDash.activeTickets', 'Active Tickets')} value={metrics?.totalActiveTickets} prefix={<BarChartOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable style={{ borderLeft: `4px solid ${token.colorSuccess}` }}>
            <Statistic title={t('execDash.avgResolution', 'Avg Resolution (Hours)')} value={metrics?.averageResolutionTimeHours} precision={1} prefix={<FieldTimeOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable style={{ borderLeft: `4px solid ${token.colorWarning}` }}>
            <Statistic title={t('execDash.fcr', 'First-time Resolution')} value={metrics?.firstContactResolutionPercentage} precision={1} suffix="%" />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable style={{ borderLeft: `4px solid ${token.colorError}` }}>
            <Statistic
              title={t('execDash.slaBreach', 'SLA Breach Rate')}
              value={metrics?.slaBreachRatePercentage}
              precision={1}
              suffix="%"
              prefix={<WarningOutlined />}
              valueStyle={{ color: token.colorError }}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={16}>
          <Card title={t('execDash.volumeTrend', 'Ticket Volume Trend (30 Days)')} style={{ height: '100%' }}>
            {trendHasData
              ? <Area data={trend} xField="date" yField="count" height={300} />
              : <Empty description={t('execDash.noTrend', 'No tickets created in this period yet')} style={{ padding: '60px 0' }} />}
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title={t('execDash.byPriority', 'Distribution by Priority')} style={{ height: '100%' }}>
            {priorityHasData
              ? <Pie data={byPriority} angleField="value" colorField="type" radius={0.8} innerRadius={0.6} height={300} legend={{ position: 'bottom' }} />
              : <Empty description={t('execDash.noData', 'No data yet')} style={{ padding: '60px 0' }} />}
          </Card>
        </Col>
      </Row>

      <Row style={{ marginTop: 24 }}>
        <Col span={24}>
          <Card>
            <div style={{ display: 'flex', alignItems: 'center' }}>
              <RobotOutlined style={{ fontSize: 32, color: token.colorPrimary, marginRight: 16 }} />
              <div>
                <Title level={4} style={{ margin: 0, color: token.colorPrimary }}>{t('execDash.aiTitle', 'AI Triage Active')}</Title>
                <Text>
                  {t('execDash.aiBody1', 'The AI triage model has analysed')}{' '}
                  <strong>{metrics?.aiPredictionsCount}</strong>{' '}
                  {t('execDash.aiBody2', 'tickets, suggesting a category, priority and confidence score for each to speed up routing. Predictions are advisory and never change a ticket on their own.')}
                </Text>
              </div>
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

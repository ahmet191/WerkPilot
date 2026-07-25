import React, { useEffect, useState } from 'react';
import { Card, Row, Col, Statistic, Spin, Typography, theme, Empty } from 'antd';
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
              ? <AreaTrend data={trend} color={token.colorPrimary} />
              : <Empty description={t('execDash.noTrend', 'No tickets created in this period yet')} style={{ padding: '60px 0' }} />}
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title={t('execDash.byPriority', 'Distribution by Priority')} style={{ height: '100%' }}>
            {priorityHasData
              ? <PriorityDonut data={byPriority} totalLabel={t('execDash.total', 'Total')} />
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

// ── Lightweight, dependency-free charts ────────────────────────────────
// Rendered as plain SVG so they paint reliably in every environment and add
// no heavy charting dependency for two simple visuals.

const PRIORITY_COLORS: Record<string, string> = {
  Critical: '#f5222d',
  High: '#fa8c16',
  Medium: '#722ed1',
  Low: '#1677ff',
};
const FALLBACK_PALETTE = ['#1677ff', '#722ed1', '#fa8c16', '#f5222d', '#13c2c2', '#52c41a'];

/** Filled area + line trend over evenly spaced points. */
const AreaTrend: React.FC<{ data: { date: string; count: number }[]; color: string }> = ({ data, color }) => {
  const W = 720, H = 300, padX = 36, padY = 24;
  const max = Math.max(1, ...data.map(d => d.count));
  const n = data.length;
  const x = (i: number) => padX + (n <= 1 ? 0 : (i * (W - 2 * padX)) / (n - 1));
  const y = (v: number) => H - padY - (v / max) * (H - 2 * padY);
  const line = data.map((d, i) => `${x(i)},${y(d.count)}`).join(' ');
  const area = `${padX},${H - padY} ${line} ${x(n - 1)},${H - padY}`;
  const gridVals = [0, 0.25, 0.5, 0.75, 1];

  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" height={300} preserveAspectRatio="xMidYMid meet" role="img">
      <defs>
        <linearGradient id="trendFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.35" />
          <stop offset="100%" stopColor={color} stopOpacity="0.02" />
        </linearGradient>
      </defs>
      {gridVals.map((g, i) => {
        const gy = padY + g * (H - 2 * padY);
        return (
          <g key={i}>
            <line x1={padX} y1={gy} x2={W - padX} y2={gy} stroke="var(--border-color)" strokeWidth="1" opacity="0.5" />
            <text x={4} y={gy + 4} fontSize="11" fill="var(--text-secondary)">{Math.round(max * (1 - g))}</text>
          </g>
        );
      })}
      <polygon points={area} fill="url(#trendFill)" />
      <polyline points={line} fill="none" stroke={color} strokeWidth="2.5" strokeLinejoin="round" strokeLinecap="round" />
      {data.map((d, i) => d.count > 0 ? <circle key={i} cx={x(i)} cy={y(d.count)} r="3" fill={color} /> : null)}
      {data.map((d, i) => (i === 0 || i === n - 1 || i === Math.floor(n / 2))
        ? <text key={`l${i}`} x={x(i)} y={H - 6} fontSize="11" fill="var(--text-secondary)" textAnchor="middle">{d.date.slice(5)}</text>
        : null)}
    </svg>
  );
};

/** Donut of proportional slices with a legend and a centre total. */
const PriorityDonut: React.FC<{ data: { type: string; value: number }[]; totalLabel: string }> = ({ data, totalLabel }) => {
  const total = data.reduce((s, d) => s + d.value, 0);
  const cx = 100, cy = 100, rOut = 82, rIn = 52;
  const polar = (r: number, deg: number): [number, number] => {
    const a = ((deg - 90) * Math.PI) / 180;
    return [cx + r * Math.cos(a), cy + r * Math.sin(a)];
  };
  const slicePath = (start: number, end: number) => {
    const sweep = Math.min(end - start, 359.99);
    const e = start + sweep;
    const [x1, y1] = polar(rOut, e), [x2, y2] = polar(rOut, start);
    const [x3, y3] = polar(rIn, start), [x4, y4] = polar(rIn, e);
    const large = sweep > 180 ? 1 : 0;
    return `M${x1} ${y1} A${rOut} ${rOut} 0 ${large} 0 ${x2} ${y2} L${x3} ${y3} A${rIn} ${rIn} 0 ${large} 1 ${x4} ${y4} Z`;
  };
  let acc = 0;
  const slices = data.map((d, i) => {
    const start = (acc / total) * 360;
    acc += d.value;
    const end = (acc / total) * 360;
    return { ...d, start, end, color: PRIORITY_COLORS[d.type] ?? FALLBACK_PALETTE[i % FALLBACK_PALETTE.length] };
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', paddingTop: 8 }}>
      <svg viewBox="0 0 200 200" width={220} height={220} role="img">
        {slices.map((s, i) => <path key={i} d={slicePath(s.start, s.end)} fill={s.color} />)}
        <text x={cx} y={cy - 4} textAnchor="middle" fontSize="30" fontWeight="700" fill="var(--text-primary)">{total}</text>
        <text x={cx} y={cy + 18} textAnchor="middle" fontSize="12" fill="var(--text-secondary)">{totalLabel}</text>
      </svg>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px 16px', justifyContent: 'center', marginTop: 16 }}>
        {slices.map((s, i) => (
          <span key={i} style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: 13, color: 'var(--text-secondary)' }}>
            <span style={{ width: 10, height: 10, borderRadius: 2, background: s.color, display: 'inline-block' }} />
            {s.type} · {s.value}
          </span>
        ))}
      </div>
    </div>
  );
};

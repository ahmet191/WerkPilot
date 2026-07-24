import React, { useEffect, useState } from 'react';
import { Card, Col, Row, Progress, Table, Statistic, Space, Typography, Button, Empty, Spin, message } from 'antd';
import { BarChartOutlined, DownloadOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text } = Typography;

interface Bucket { count: number; }
interface StatusRow extends Bucket { status: string; }
interface PriorityRow extends Bucket { priority: string; }
interface TrendPoint { date: string; count: number; }
interface SlaSummary { total: number; compliant: number; breached: number; compliancePct: number; }
interface TeamRow { agent: string; assigned: number; open: number; resolved: number; }

const PRIORITY_COLOR: Record<string, string> = {
  Critical: '#cf1322', High: '#fa8c16', Medium: '#1677ff', Low: '#52c41a',
};
const STATUS_COLOR: Record<string, string> = {
  New: '#1677ff', InProgress: '#fa8c16', WaitingForApproval: '#722ed1', Resolved: '#52c41a', Closed: '#8c8c8c',
};

/** Horizontal labelled bar built from AntD Progress — no chart dependency. */
const BarRow: React.FC<{ label: string; value: number; max: number; color?: string }> = ({ label, value, max, color }) => (
  <div style={{ marginBottom: 10 }}>
    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 2 }}>
      <Text>{label}</Text><Text strong>{value}</Text>
    </div>
    <Progress percent={max ? Math.round((value / max) * 100) : 0} showInfo={false} strokeColor={color} />
  </div>
);

export const ReportsPage: React.FC = () => {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState<StatusRow[]>([]);
  const [priority, setPriority] = useState<PriorityRow[]>([]);
  const [trend, setTrend] = useState<TrendPoint[]>([]);
  const [sla, setSla] = useState<SlaSummary | null>(null);
  const [team, setTeam] = useState<TeamRow[]>([]);
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const [s, p, tr, sl, tm] = await Promise.all([
          api.get('/analytics/reports/tickets-by-status'),
          api.get('/analytics/reports/tickets-by-priority'),
          api.get('/analytics/reports/volume-trend?days=30'),
          api.get('/analytics/reports/sla-compliance'),
          api.get('/analytics/reports/team-performance'),
        ]);
        setStatus(s.data); setPriority(p.data); setTrend(tr.data); setSla(sl.data); setTeam(tm.data);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const exportCsv = async () => {
    setExporting(true);
    try {
      const res = await api.get('/analytics/reports/export/tickets.csv', { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a');
      a.href = url; a.download = 'tickets.csv'; a.click();
      URL.revokeObjectURL(url);
    } catch {
      message.error(t('reports.exportFailed', 'Export failed'));
    } finally {
      setExporting(false);
    }
  };

  if (loading) return <div style={{ textAlign: 'center', padding: 80 }}><Spin size="large" /></div>;

  const statusMax = Math.max(1, ...status.map(r => r.count));
  const priorityMax = Math.max(1, ...priority.map(r => r.count));
  const trendMax = Math.max(1, ...trend.map(p => p.count));

  return (
    <div>
      <Space align="center" style={{ marginBottom: 18, justifyContent: 'space-between', width: '100%' }}>
        <Space align="center">
          <BarChartOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
          <Title level={3} style={{ margin: 0 }}>{t('reports.title', 'Reports & Analytics')}</Title>
        </Space>
        <Button icon={<DownloadOutlined />} onClick={exportCsv} loading={exporting}>
          {t('reports.exportCsv', 'Export tickets CSV')}
        </Button>
      </Space>

      <Row gutter={[16, 16]}>
        <Col xs={24} md={8}>
          <Card title={t('reports.slaCompliance', 'SLA compliance')} styles={{ body: { textAlign: 'center' } }}>
            <Progress type="dashboard" percent={sla?.compliancePct ?? 100}
              strokeColor={(sla?.compliancePct ?? 100) >= 90 ? '#52c41a' : (sla?.compliancePct ?? 0) >= 70 ? '#fa8c16' : '#cf1322'} />
            <div style={{ marginTop: 12 }}>
              <Space size="large">
                <Statistic title={t('reports.compliant', 'Within SLA')} value={sla?.compliant ?? 0} valueStyle={{ color: '#52c41a' }} />
                <Statistic title={t('reports.breached', 'Breached')} value={sla?.breached ?? 0} valueStyle={{ color: '#cf1322' }} />
              </Space>
            </div>
          </Card>
        </Col>

        <Col xs={24} md={8}>
          <Card title={t('reports.byStatus', 'Tickets by status')}>
            {status.length === 0 ? <Empty /> : status.map(r => (
              <BarRow key={r.status} label={t(`ticketState.${r.status}`, r.status)} value={r.count} max={statusMax} color={STATUS_COLOR[r.status]} />
            ))}
          </Card>
        </Col>

        <Col xs={24} md={8}>
          <Card title={t('reports.byPriority', 'Tickets by priority')}>
            {priority.length === 0 ? <Empty /> : priority.map(r => (
              <BarRow key={r.priority} label={t(`priority.${r.priority}`, r.priority)} value={r.count} max={priorityMax} color={PRIORITY_COLOR[r.priority]} />
            ))}
          </Card>
        </Col>

        <Col xs={24}>
          <Card title={t('reports.volumeTrend', 'Ticket volume — last 30 days')}>
            <div style={{ display: 'flex', alignItems: 'flex-end', gap: 3, height: 120 }}>
              {trend.map(p => (
                <div key={p.date} title={`${p.date}: ${p.count}`}
                  style={{ flex: 1, height: `${(p.count / trendMax) * 100}%`, minHeight: p.count ? 4 : 1,
                    background: p.count ? 'var(--accent-color)' : 'var(--border-color, #eee)', borderRadius: 2 }} />
              ))}
            </div>
            <Text type="secondary" style={{ fontSize: 12 }}>{trend[0]?.date} → {trend[trend.length - 1]?.date}</Text>
          </Card>
        </Col>

        <Col xs={24}>
          <Card title={t('reports.teamPerformance', 'Team performance')}>
            <Table<TeamRow>
              rowKey="agent"
              dataSource={team}
              pagination={false}
              columns={[
                { title: t('reports.agent', 'Agent'), dataIndex: 'agent' },
                { title: t('reports.assigned', 'Assigned'), dataIndex: 'assigned', sorter: (a, b) => a.assigned - b.assigned },
                { title: t('reports.open', 'Open'), dataIndex: 'open' },
                { title: t('reports.resolved', 'Resolved'), dataIndex: 'resolved',
                  render: (v: number, r) => <Text>{v} <Text type="secondary">({r.assigned ? Math.round((v / r.assigned) * 100) : 0}%)</Text></Text> },
              ]}
            />
          </Card>
        </Col>
      </Row>
    </div>
  );
};

import { useEffect, useState } from 'react';
import { Timeline, Tag, Typography, Spin, Empty } from 'antd';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  ClockCircleOutlined,
  MinusCircleOutlined,
} from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

interface TimelineStep {
  order: number;
  approverType: string;
  approverRole?: string;
  approverName?: string;
  status: string;
  actedByName?: string;
  actedAt?: string;
  comment?: string;
}

interface TimelineEntry {
  id: string;
  status: string;
  currentStepOrder: number;
  requestedByName: string;
  createdAt: string;
  completedAt?: string;
  steps: TimelineStep[];
}

const STATUS_COLORS: Record<string, string> = {
  Pending: 'gold',
  Approved: 'green',
  Rejected: 'red',
  Cancelled: 'default',
};

function stepDot(status: string) {
  switch (status) {
    case 'Approved': return <CheckCircleOutlined style={{ color: '#52c41a' }} />;
    case 'Rejected': return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
    case 'Cancelled': return <MinusCircleOutlined style={{ color: '#8c8c8c' }} />;
    default: return <ClockCircleOutlined style={{ color: '#faad14' }} />;
  }
}

/** Read-only approval history for a governed entity; embedded in detail views. */
export function ApprovalTimeline({ entityType, entityId }: { entityType: string; entityId: string }) {
  const { t } = useTranslation();
  const [entries, setEntries] = useState<TimelineEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    api.get(`/approvals/entity/${entityType}/${entityId}`)
      .then(res => { if (!cancelled) setEntries(res.data); })
      .catch(() => { /* toast handled globally */ })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [entityType, entityId]);

  if (loading) return <Spin size="small" style={{ display: 'block', margin: '12px auto' }} />;
  if (entries.length === 0) return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('approvals.none', 'No approval history')} />;

  return (
    <>
      {entries.map(entry => (
        <div key={entry.id} style={{ marginBottom: 16 }}>
          <div style={{ marginBottom: 8, display: 'flex', gap: 8, alignItems: 'center' }}>
            <Tag color={STATUS_COLORS[entry.status] ?? 'blue'}>{entry.status}</Tag>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {t('approvals.requestedBy', 'Requested by')} {entry.requestedByName} · {new Date(entry.createdAt).toLocaleString()}
            </Typography.Text>
          </div>
          <Timeline
            items={entry.steps.map(step => ({
              dot: stepDot(step.status),
              children: (
                <div>
                  <Typography.Text strong>
                    {t('approvals.step', 'Step')} {step.order}:{' '}
                    {step.approverName ?? step.approverRole ?? step.approverType}
                  </Typography.Text>
                  <Tag style={{ marginLeft: 8 }} color={STATUS_COLORS[step.status] ?? 'blue'}>{step.status}</Tag>
                  {step.actedByName && (
                    <div style={{ fontSize: 12, opacity: 0.75 }}>
                      {step.actedByName} · {step.actedAt ? new Date(step.actedAt).toLocaleString() : ''}
                      {step.comment && <div style={{ fontStyle: 'italic' }}>"{step.comment}"</div>}
                    </div>
                  )}
                </div>
              ),
            }))}
          />
        </div>
      ))}
    </>
  );
}

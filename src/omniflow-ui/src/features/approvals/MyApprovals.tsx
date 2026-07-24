import { useCallback, useEffect, useState } from 'react';
import { Table, Button, Tag, Space, Modal, Radio, Input, message, Tooltip, Typography } from 'antd';
import { ReloadOutlined, CheckOutlined, CloseOutlined, AuditOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';
import { ApprovalTimeline } from './ApprovalTimeline';

interface PendingApproval {
  id: string;
  entityType: string;
  entityId: string;
  entityDisplay: string;
  requestedByName: string;
  stepOrder: number;
  totalSteps: number;
  approverRole?: string;
  createdAt: string;
}

const ENTITY_COLORS: Record<string, string> = {
  Ticket: 'blue',
  LeaveRequest: 'purple',
};

/** Inbox of approval steps waiting on the current user, with a decide modal. */
export function MyApprovals() {
  const { t } = useTranslation();
  const [items, setItems] = useState<PendingApproval[]>([]);
  const [loading, setLoading] = useState(true);
  const [deciding, setDeciding] = useState<PendingApproval | null>(null);
  const [approve, setApprove] = useState(true);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get('/approvals/mine');
      setItems(res.data);
    } catch {
      // toast handled globally
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openDecide = (record: PendingApproval, initialApprove: boolean) => {
    setDeciding(record);
    setApprove(initialApprove);
    setComment('');
  };

  const submitDecision = async () => {
    if (!deciding) return;
    setSubmitting(true);
    try {
      await api.post(`/approvals/${deciding.id}/decide`, { approve, comment: comment || null });
      message.success(approve
        ? t('approvals.approved', 'Approved')
        : t('approvals.rejected', 'Rejected'));
      setDeciding(null);
      await load();
    } catch {
      // toast handled globally
    } finally {
      setSubmitting(false);
    }
  };

  const columns = [
    {
      title: t('approvals.entity', 'Item'),
      dataIndex: 'entityDisplay',
      key: 'entityDisplay',
      render: (text: string, record: PendingApproval) => (
        <Space>
          <Tag color={ENTITY_COLORS[record.entityType] ?? 'default'}>
            {t(`approvals.type.${record.entityType}`, record.entityType)}
          </Tag>
          <Typography.Text>{text}</Typography.Text>
        </Space>
      ),
    },
    {
      title: t('approvals.requestedBy', 'Requested by'),
      dataIndex: 'requestedByName',
      key: 'requestedByName',
      width: 180,
    },
    {
      title: t('approvals.stepCol', 'Step'),
      key: 'step',
      width: 90,
      render: (_: unknown, record: PendingApproval) => `${record.stepOrder} / ${record.totalSteps}`,
    },
    {
      title: t('approvals.created', 'Created'),
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (v: string) => new Date(v).toLocaleString(),
    },
    {
      title: t('approvals.decision', 'Decision'),
      key: 'actions',
      width: 200,
      render: (_: unknown, record: PendingApproval) => (
        <Space>
          <Button size="small" type="primary" icon={<CheckOutlined />} onClick={() => openDecide(record, true)}>
            {t('approvals.approve', 'Approve')}
          </Button>
          <Button size="small" danger icon={<CloseOutlined />} onClick={() => openDecide(record, false)}>
            {t('approvals.reject', 'Reject')}
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div className="enterprise-grid">
      <div className="grid-toolbar">
        <Space>
          <AuditOutlined style={{ color: 'var(--accent-color)' }} />
          <h3 className="grid-title">
            {t('approvals.title', 'My Approvals')}
          </h3>
          {items.length > 0 && <Tag color="gold" style={{ borderRadius: 999 }}>{items.length}</Tag>}
        </Space>
        <Tooltip title={t('grid.refresh', 'Refresh')}>
          <Button size="small" icon={<ReloadOutlined />} onClick={load} loading={loading} />
        </Tooltip>
      </div>

      <Table<PendingApproval>
        size="small"
        rowKey="id"
        dataSource={items}
        columns={columns}
        loading={loading}
        pagination={false}
        locale={{ emptyText: t('approvals.empty', 'Nothing waiting for your approval 🎉') }}
      />

      <Modal
        open={deciding !== null}
        title={deciding?.entityDisplay}
        onCancel={() => setDeciding(null)}
        onOk={submitDecision}
        confirmLoading={submitting}
        okText={approve ? t('approvals.approve', 'Approve') : t('approvals.reject', 'Reject')}
        okButtonProps={{ danger: !approve }}
        destroyOnHidden
      >
        {deciding && (
          <>
            <Radio.Group
              value={approve}
              onChange={e => setApprove(e.target.value)}
              style={{ marginBottom: 12 }}
              options={[
                { label: t('approvals.approve', 'Approve'), value: true },
                { label: t('approvals.reject', 'Reject'), value: false },
              ]}
              optionType="button"
              buttonStyle="solid"
            />
            <Input.TextArea
              rows={3}
              maxLength={1000}
              placeholder={t('approvals.commentPlaceholder', 'Comment (optional)')}
              value={comment}
              onChange={e => setComment(e.target.value)}
              style={{ marginBottom: 16 }}
            />
            <Typography.Title level={5} style={{ marginTop: 0 }}>
              {t('approvals.history', 'Approval history')}
            </Typography.Title>
            <ApprovalTimeline entityType={deciding.entityType} entityId={deciding.entityId} />
          </>
        )}
      </Modal>
    </div>
  );
}

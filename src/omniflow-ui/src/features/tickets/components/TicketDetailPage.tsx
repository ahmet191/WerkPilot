import React, { useCallback, useEffect, useState } from 'react';
import {
  Button, Card, Col, Descriptions, Divider, Empty, Input, List, Row, Space,
  Spin, Tag, Timeline, Typography, Upload, message,
} from 'antd';
import type { UploadProps } from 'antd';
import {
  ArrowLeftOutlined, PaperClipOutlined, DownloadOutlined, DeleteOutlined,
  UploadOutlined, SendOutlined, HistoryOutlined, CommentOutlined,
} from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../../../core/api';
import { useAuth } from '../../../core/AuthContext';
import { usePermissions, Perm } from '../../../core/PermissionContext';
import { useTranslation } from '../../../core/LanguageContext';

const { Title, Text, Paragraph } = Typography;

interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByName: string;
  createdAt: string;
}

interface Comment {
  id: string;
  authorName: string;
  content: string;
  createdAt: string;
}

interface HistoryEntry {
  action: string;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  changedByName: string;
  changedAt: string;
}

interface TicketDetails {
  id: string;
  title: string;
  description: string;
  type: string;
  priority: string;
  impact: string;
  urgency: string;
  currentState: string;
  requesterName: string;
  assigneeName: string;
  assigneeId?: string;
  slaDueDate?: string;
  createdAt: string;
  templateName: string;
  dynamicData: Record<string, unknown>;
  availableTriggers: string[];
  comments: Comment[];
  history: HistoryEntry[];
  attachments: Attachment[];
}

const STATE_COLOR: Record<string, string> = {
  New: 'blue', InProgress: 'gold', WaitingForApproval: 'purple', Resolved: 'green', Closed: 'default',
};
const PRIORITY_COLOR: Record<string, string> = {
  Critical: 'red', High: 'volcano', Medium: 'gold', Low: 'default',
};

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Permanent, linkable ticket workspace: fields, transitions, comments, files, history. */
export const TicketDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { user } = useAuth();
  const { has } = usePermissions();

  const [ticket, setTicket] = useState<TicketDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [comment, setComment] = useState('');
  const [busy, setBusy] = useState(false);

  const canEdit = has(Perm.TicketsEdit);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      const res = await api.get(`/tickets/${id}`);
      setTicket(res.data);
    } catch {
      setNotFound(true);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const runTrigger = async (trigger: string) => {
    setBusy(true);
    try {
      await api.post(`/tickets/${id}/transition`, { trigger, userId: user?.id });
      message.success(t('ticket.transitioned', 'State updated'));
      await load();
    } catch {
      // toast handled globally
    } finally {
      setBusy(false);
    }
  };

  const addComment = async () => {
    if (!comment.trim()) return;
    setBusy(true);
    try {
      await api.post(`/tickets/${id}/comments`, { authorId: user?.id, content: comment.trim() });
      setComment('');
      await load();
    } catch {
      // toast handled globally
    } finally {
      setBusy(false);
    }
  };

  const downloadAttachment = async (att: Attachment) => {
    try {
      const res = await api.get(`/tickets/attachments/${att.id}/download`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data as Blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = att.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch {
      // toast handled globally
    }
  };

  const deleteAttachment = async (att: Attachment) => {
    try {
      await api.delete(`/tickets/attachments/${att.id}`);
      message.success(t('ticket.attachmentDeleted', 'Attachment removed'));
      await load();
    } catch {
      // toast handled globally
    }
  };

  const handleCustomUpload: UploadProps['customRequest'] = async (options) => {
    const { file, onSuccess, onError } = options;
    const formData = new FormData();
    formData.append('file', file);
    try {
      await api.post(`/tickets/${id}/attachments`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      onSuccess?.('ok');
      message.success(t('ticket.uploaded', 'File uploaded'));
      load();
    } catch (err: any) {
      onError?.(err);
      message.error(t('ticket.uploadFailed', 'Upload failed'));
    }
  };

  const uploadProps: UploadProps = {
    customRequest: handleCustomUpload,
    showUploadList: false,
  };

  if (loading) {
    return <div style={{ textAlign: 'center', padding: 80 }}><Spin size="large" /></div>;
  }

  if (notFound || !ticket) {
    return (
      <Empty
        description={t('ticket.notFound', 'Ticket not found')}
        style={{ marginTop: 80 }}
      >
        <Button type="primary" onClick={() => navigate('/tickets')}>{t('ticket.backToList', 'Back to tickets')}</Button>
      </Empty>
    );
  }

  // Dynamic fields not already shown as first-class columns.
  const knownKeys = new Set(['Title', 'Description', 'Type', 'Priority', 'Impact', 'Urgency']);
  const extraFields = Object.entries(ticket.dynamicData).filter(([k]) => !knownKeys.has(k));

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto' }}>
      <Space style={{ marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/tickets')}>
          {t('ticket.backToList', 'Back to tickets')}
        </Button>
      </Space>

      <div className="grid-toolbar" style={{ borderRadius: 'var(--radius-md)', marginBottom: 18 }}>
        <Space direction="vertical" size={2}>
          <Space wrap>
            <Title level={4} style={{ margin: 0 }}>{ticket.title}</Title>
            <Tag color={STATE_COLOR[ticket.currentState] ?? 'default'}>{ticket.currentState}</Tag>
            <Tag color={PRIORITY_COLOR[ticket.priority] ?? 'default'}>{ticket.priority}</Tag>
          </Space>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {ticket.templateName} · {t('ticket.requestedBy', 'Requested by')} {ticket.requesterName}
          </Text>
        </Space>
        {canEdit && ticket.availableTriggers.length > 0 && (
          <Space wrap>
            {ticket.availableTriggers.map(trigger => (
              <Button key={trigger} type="primary" ghost loading={busy} onClick={() => runTrigger(trigger)}>
                {t(`ticket.trigger.${trigger}`, trigger)}
              </Button>
            ))}
          </Space>
        )}
      </div>

      <Row gutter={[18, 18]}>
        <Col xs={24} lg={15}>
          <Card size="small" title={t('ticket.details', 'Details')} className="card-lift" style={{ marginBottom: 18 }}>
            <Descriptions column={2} size="small">
              <Descriptions.Item label={t('ticket.type', 'Type')}>{ticket.type}</Descriptions.Item>
              <Descriptions.Item label={t('ticket.impact', 'Impact')}>{ticket.impact}</Descriptions.Item>
              <Descriptions.Item label={t('ticket.urgency', 'Urgency')}>{ticket.urgency}</Descriptions.Item>
              <Descriptions.Item label={t('ticket.assignee', 'Assignee')}>
                {ticket.assigneeName || <Text type="secondary">—</Text>}
              </Descriptions.Item>
              <Descriptions.Item label={t('ticket.sla', 'SLA due')}>
                {ticket.slaDueDate ? new Date(ticket.slaDueDate).toLocaleString() : <Text type="secondary">—</Text>}
              </Descriptions.Item>
              <Descriptions.Item label={t('ticket.created', 'Created')}>
                {new Date(ticket.createdAt).toLocaleString()}
              </Descriptions.Item>
              {extraFields.map(([key, value]) => (
                <Descriptions.Item key={key} label={key}>{String(value ?? '')}</Descriptions.Item>
              ))}
            </Descriptions>
            {ticket.description && (
              <>
                <Divider style={{ margin: '12px 0' }} />
                <Paragraph style={{ marginBottom: 0, whiteSpace: 'pre-wrap' }}>{ticket.description}</Paragraph>
              </>
            )}
          </Card>

          <Card
            size="small"
            title={<Space><CommentOutlined /> {t('ticket.comments', 'Comments')} ({ticket.comments.length})</Space>}
            className="card-lift"
          >
            {ticket.comments.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('ticket.noComments', 'No comments yet')} />
            ) : (
              <List
                dataSource={ticket.comments}
                renderItem={c => (
                  <List.Item>
                    <List.Item.Meta
                      title={
                        <Space size={8}>
                          <span style={{ fontWeight: 600, fontSize: 13 }}>{c.authorName}</span>
                          <Text type="secondary" style={{ fontSize: 11 }}>{new Date(c.createdAt).toLocaleString()}</Text>
                        </Space>
                      }
                      description={<span style={{ whiteSpace: 'pre-wrap' }}>{c.content}</span>}
                    />
                  </List.Item>
                )}
              />
            )}
            <Divider style={{ margin: '12px 0' }} />
            <Space.Compact style={{ width: '100%' }}>
              <Input.TextArea
                rows={2}
                value={comment}
                onChange={e => setComment(e.target.value)}
                placeholder={t('ticket.addComment', 'Add a comment…')}
                maxLength={2000}
              />
              <Button type="primary" icon={<SendOutlined />} loading={busy} disabled={!comment.trim()} onClick={addComment}>
                {t('ticket.send', 'Send')}
              </Button>
            </Space.Compact>
          </Card>
        </Col>

        <Col xs={24} lg={9}>
          <Card
            size="small"
            title={<Space><PaperClipOutlined /> {t('ticket.attachments', 'Attachments')} ({ticket.attachments.length})</Space>}
            className="card-lift"
            style={{ marginBottom: 18 }}
            extra={(
              <Upload {...uploadProps}>
                <Button size="small" icon={<UploadOutlined />}>{t('ticket.upload', 'Upload')}</Button>
              </Upload>
            )}
          >
            {ticket.attachments.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('ticket.noAttachments', 'No files attached')} />
            ) : (
              <List
                size="small"
                dataSource={ticket.attachments}
                renderItem={att => (
                  <List.Item
                    actions={[
                      <Button key="dl" type="text" size="small" icon={<DownloadOutlined />} onClick={() => downloadAttachment(att)} />,
                      ...(canEdit ? [<Button key="rm" type="text" size="small" danger icon={<DeleteOutlined />} onClick={() => deleteAttachment(att)} />] : []),
                    ]}
                  >
                    <List.Item.Meta
                      avatar={<PaperClipOutlined style={{ color: 'var(--accent-color)' }} />}
                      title={<span style={{ fontSize: 13 }}>{att.fileName}</span>}
                      description={
                        <Text type="secondary" style={{ fontSize: 11 }}>
                          {formatBytes(att.sizeBytes)} · {att.uploadedByName}
                        </Text>
                      }
                    />
                  </List.Item>
                )}
              />
            )}
          </Card>

          <Card
            size="small"
            title={<Space><HistoryOutlined /> {t('ticket.history', 'Activity')}</Space>}
            className="card-lift"
          >
            {ticket.history.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('ticket.noHistory', 'No activity')} />
            ) : (
              <Timeline
                items={ticket.history.map(h => ({
                  children: (
                    <div>
                      <div style={{ fontSize: 13 }}>
                        <strong>{t(`ticket.action.${h.action}`, h.action)}</strong>
                        {h.newValue && <span> — {h.newValue}</span>}
                      </div>
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        {h.changedByName} · {new Date(h.changedAt).toLocaleString()}
                      </Text>
                    </div>
                  ),
                }))}
              />
            )}
          </Card>
        </Col>
      </Row>
    </div>
  );
};

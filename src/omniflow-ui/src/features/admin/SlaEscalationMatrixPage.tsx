import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, InputNumber, Switch, Row, Col, message, Spin, Popconfirm, Space } from 'antd';
import { FieldTimeOutlined, PlusOutlined, AlertOutlined, BellOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

interface SlaRule {
  id: string;
  name: string;
  priority: string;
  responseTimeMinutes: number;
  resolutionTimeHours: number;
  escalateAfterPercent: number;
  escalateToUserId?: string;
  escalateToUserName?: string;
  notifyManagerOnBreach: boolean;
  isActive: boolean;
}

export const SlaEscalationMatrixPage: React.FC = () => {
  const { t } = useTranslation();
  const [rules, setRules] = useState<SlaRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<SlaRule | null>(null);
  const [form] = Form.useForm();

  const fetchRules = async () => {
    setLoading(true);
    try {
      const res = await api.get('/slas/policies');
      setRules(res.data);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRules();
  }, []);

  const handleOpenAddModal = () => {
    setEditingRule(null);
    form.resetFields();
    form.setFieldsValue({ priority: 'Medium', responseTimeMinutes: 60, resolutionTimeHours: 24, escalateAfterPercent: 80, notifyManagerOnBreach: true, isActive: true });
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (rule: SlaRule) => {
    setEditingRule(rule);
    form.setFieldsValue(rule);
    setIsModalOpen(true);
  };

  const handleSubmit = async (values: any) => {
    try {
      const payload = { ...values, id: editingRule?.id };
      await api.post('/slas/policies', payload);
      message.success(editingRule ? t('slam.updated', 'SLA policy rule updated.') : t('slam.saved', 'SLA policy rule saved.'));
      setIsModalOpen(false);
      fetchRules();
    } catch {
      message.error(t('slam.saveFailed', 'Failed to save SLA policy.'));
    }
  };

  const handleDeleteRule = async (id: string) => {
    try {
      await api.delete(`/slas/policies/${id}`);
      message.success(t('slam.deleted', 'SLA policy rule deleted.'));
      fetchRules();
    } catch {
      message.error(t('slam.deleteFailed', 'Failed to delete SLA policy rule.'));
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <FieldTimeOutlined style={{ marginRight: 8, color: '#fa8c16' }} />
              {t('slam.title', 'Advanced SLA Policy Engine & Escalation Matrix')}
            </h2>
            <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
              {t('slam.subtitle', 'Configure response & resolution SLAs, breach warnings, and automatic manager escalation rules.')}
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenAddModal}>
            {t('slam.addRule', 'Add SLA Policy Rule')}
          </Button>
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={rules}
            rowKey="id"
            columns={[
              {
                title: t('slam.colPriority', 'Priority'),
                dataIndex: 'priority',
                render: (prio: string) => {
                  if (prio === 'Critical') return <Tag color="red">{t('slam.pCritical', 'Critical')} P1</Tag>;
                  if (prio === 'High') return <Tag color="orange">{t('slam.pHigh', 'High')} P2</Tag>;
                  if (prio === 'Medium') return <Tag color="blue">{t('slam.pMedium', 'Medium')} P3</Tag>;
                  return <Tag color="default">{t('slam.pLow', 'Low')} P4</Tag>;
                }
              },
              { title: t('slam.colPolicyName', 'Policy Name'), dataIndex: 'name', render: (n: string) => <strong>{n}</strong> },
              {
                title: t('slam.colTargetSla', 'Target SLA'),
                key: 'sla',
                render: (_: any, record: SlaRule) => (
                  <div>
                    <div>{t('slam.response', 'Response')}: <strong>{record.responseTimeMinutes} {t('slam.mins', 'mins')}</strong></div>
                    <div>{t('slam.resolution', 'Resolution')}: <strong>{record.resolutionTimeHours} {t('slam.hours', 'hours')}</strong></div>
                  </div>
                )
              },
              {
                title: t('slam.colEscalation', 'Escalation Threshold'),
                dataIndex: 'escalateAfterPercent',
                render: (pct: number) => <Tag color="warning" icon={<AlertOutlined />}>{t('slam.escalateAt', 'Escalate at {n}% SLA').replace('{n}', String(pct))}</Tag>
              },
              {
                title: t('slam.colBreachNotif', 'Breach Notification'),
                dataIndex: 'notifyManagerOnBreach',
                render: (notify: boolean) => notify ? <Tag color="green" icon={<BellOutlined />}>{t('slam.notifyManager', 'Notify Manager')}</Tag> : <Tag color="default">{t('slam.noNotify', 'No Manager Notification')}</Tag>
              },
              {
                title: t('slam.colStatus', 'Status'),
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">{t('slam.active', 'Active')}</Tag> : <Tag color="default">{t('slam.inactive', 'Inactive')}</Tag>
              },
              {
                title: t('slam.colActions', 'Actions'),
                key: 'actions',
                render: (_: any, record: SlaRule) => (
                  <Space size="small">
                    <Button type="text" style={{ color: '#1890ff' }} icon={<EditOutlined />} onClick={() => handleOpenEditModal(record)} title={t('slam.editRule', 'Edit Rule')} />
                    <Popconfirm title={t('slam.confirmDelete', 'Delete this SLA rule?')} onConfirm={() => handleDeleteRule(record.id)} okText={t('common.delete', 'Delete')} cancelText={t('common.cancel', 'Cancel')}>
                      <Button type="text" danger icon={<DeleteOutlined />} title={t('slam.deleteRule', 'Delete Rule')} />
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title={editingRule ? t('slam.editModal', 'Edit SLA Policy Rule') : t('slam.configModal', 'Configure SLA Policy Rule')}
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item name="name" label={t('slam.policyName', 'Policy Name')} rules={[{ required: true }]}>
            <Input placeholder={t('slam.policyNamePlaceholder', 'e.g. Critical P1 Outage SLA Policy')} />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="priority" label={t('slam.ticketPriority', 'Ticket Priority')}>
                <Select options={[
                  { value: 'Critical', label: t('slam.optCritical', 'Critical (P1)') },
                  { value: 'High', label: t('slam.optHigh', 'High (P2)') },
                  { value: 'Medium', label: t('slam.optMedium', 'Medium (P3)') },
                  { value: 'Low', label: t('slam.optLow', 'Low (P4)') }
                ]} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="escalateAfterPercent" label={t('slam.threshold', 'Escalation Threshold (%)')}>
                <InputNumber min={50} max={99} style={{ width: '100%' }} addonAfter="%" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="responseTimeMinutes" label={t('slam.responseSla', 'Response SLA (Mins)')}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="resolutionTimeHours" label={t('slam.resolutionSla', 'Resolution SLA (Hours)')}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="notifyManagerOnBreach" label={t('slam.notifyOnBreach', 'Notify Manager on Breach')} valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="isActive" label={t('slam.isActive', 'Is Policy Active')} valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit">{t('slam.savePolicy', 'Save SLA Policy')}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

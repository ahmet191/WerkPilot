import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, InputNumber, Switch, Row, Col, message, Spin } from 'antd';
import { FieldTimeOutlined, PlusOutlined, AlertOutlined, BellOutlined } from '@ant-design/icons';
import { api } from '../../core/api';

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
  const [rules, setRules] = useState<SlaRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();

  const fetchRules = async () => {
    setLoading(true);
    try {
      const res = await api.get('/slas/policies');
      setRules(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRules();
  }, []);

  const handleSubmit = async (values: any) => {
    try {
      await api.post('/slas/policies', values);
      message.success('SLA policy rule saved successfully!');
      setIsModalOpen(false);
      form.resetFields();
      fetchRules();
    } catch (err) {
      message.error('Failed to save SLA policy.');
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <FieldTimeOutlined style={{ marginRight: 8, color: '#fa8c16' }} />
              Advanced SLA Policy Engine & Escalation Matrix
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Configure response & resolution SLAs, breach warnings, and automatic manager escalation rules.
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
            Add SLA Policy Rule
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
                title: 'Priority',
                dataIndex: 'priority',
                render: (prio: string) => {
                  if (prio === 'Critical') return <Tag color="red">Critical P1</Tag>;
                  if (prio === 'High') return <Tag color="orange">High P2</Tag>;
                  if (prio === 'Medium') return <Tag color="blue">Medium P3</Tag>;
                  return <Tag color="default">Low P4</Tag>;
                }
              },
              { title: 'Policy Name', dataIndex: 'name', render: (n: string) => <strong>{n}</strong> },
              {
                title: 'Target SLA',
                key: 'sla',
                render: (_: any, record: SlaRule) => (
                  <div>
                    <div>Response: <strong>{record.responseTimeMinutes} mins</strong></div>
                    <div>Resolution: <strong>{record.resolutionTimeHours} hours</strong></div>
                  </div>
                )
              },
              {
                title: 'Escalation Threshold',
                dataIndex: 'escalateAfterPercent',
                render: (pct: number) => <Tag color="warning" icon={<AlertOutlined />}>Escalate at {pct}% SLA</Tag>
              },
              {
                title: 'Breach Notification',
                dataIndex: 'notifyManagerOnBreach',
                render: (notify: boolean) => notify ? <Tag color="green" icon={<BellOutlined />}>Notify Manager</Tag> : <Tag color="default">No Manager Notification</Tag>
              },
              {
                title: 'Status',
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">Active</Tag> : <Tag color="default">Inactive</Tag>
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title="Configure SLA Policy Rule & Escalation Matrix"
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ priority: 'Medium', responseTimeMinutes: 60, resolutionTimeHours: 24, escalateAfterPercent: 80, notifyManagerOnBreach: true, isActive: true }}>
          <Form.Item name="name" label="Policy Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Critical P1 Outage SLA Policy" />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="priority" label="Ticket Priority">
                <Select options={[
                  { value: 'Critical', label: 'Critical (P1)' },
                  { value: 'High', label: 'High (P2)' },
                  { value: 'Medium', label: 'Medium (P3)' },
                  { value: 'Low', label: 'Low (P4)' }
                ]} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="escalateAfterPercent" label="Escalation Threshold (%)">
                <InputNumber min={50} max={99} style={{ width: '100%' }} addonAfter="%" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="responseTimeMinutes" label="Response SLA (Mins)">
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="resolutionTimeHours" label="Resolution SLA (Hours)">
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="notifyManagerOnBreach" label="Notify Manager on Breach" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="isActive" label="Is Policy Active" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit">Save SLA Policy</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

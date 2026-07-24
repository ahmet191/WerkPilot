import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Progress, Button, Modal, Form, Input, Select, InputNumber, Row, Col, Statistic, message, Spin } from 'antd';
import { AimOutlined, PlusOutlined, CheckCircleOutlined, ClockCircleOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';

interface EmployeeGoal {
  id: string;
  employeeUserId: string;
  title: string;
  description: string;
  quarter: string;
  year: number;
  targetValue: number;
  currentValue: number;
  unit: string;
  progressPercent: number;
  status: string;
}

export const PerformanceOkrPage: React.FC = () => {
  const [goals, setGoals] = useState<EmployeeGoal[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();

  const fetchGoals = async () => {
    setLoading(true);
    try {
      const res = await api.get('/hr/goals');
      setGoals(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGoals();
  }, []);

  const handleGoalSubmit = async (values: any) => {
    try {
      await api.post('/hr/goals', values);
      message.success('Goal updated successfully!');
      setIsModalOpen(false);
      form.resetFields();
      fetchGoals();
    } catch (err) {
      message.error('Failed to save goal.');
    }
  };

  const achievedCount = goals.filter(g => g.status === 'Achieved' || g.progressPercent >= 100).length;
  const avgProgress = goals.length ? Math.round(goals.reduce((acc, g) => acc + g.progressPercent, 0) / goals.length) : 0;

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <AimOutlined style={{ marginRight: 8, color: '#1890ff' }} />
              Performance & OKR Goal Tracking
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Track quarterly (Q1-Q4) and annual Objectives & Key Results (OKRs).
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
            Add New Goal / OKR
          </Button>
        </div>

        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title="Total OKRs / Goals" value={goals.length} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title="Achieved Goals" value={achievedCount} valueStyle={{ color: '#52c41a' }} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <div style={{ fontSize: 14, color: '#8c8c8c', marginBottom: 4 }}>Average OKR Completion</div>
              <Progress percent={avgProgress} status="active" />
            </Card>
          </Col>
        </Row>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={goals}
            rowKey="id"
            columns={[
              {
                title: 'Quarter / Year',
                dataIndex: 'quarter',
                render: (q: string, record: EmployeeGoal) => (
                  <Tag color="blue">{q} {record.year}</Tag>
                )
              },
              { title: 'Goal Title', dataIndex: 'title', render: (t: string) => <strong>{t}</strong> },
              { title: 'Description', dataIndex: 'description' },
              {
                title: 'Progress',
                dataIndex: 'progressPercent',
                render: (pct: number, record: EmployeeGoal) => (
                  <div style={{ width: 160 }}>
                    <Progress percent={pct} status={pct >= 100 ? 'success' : 'active'} />
                    <span style={{ fontSize: 11, color: '#8c8c8c' }}>{record.currentValue} / {record.targetValue} {record.unit}</span>
                  </div>
                )
              },
              {
                title: 'Status',
                dataIndex: 'status',
                render: (st: string) => {
                  if (st === 'Achieved') return <Tag color="green" icon={<CheckCircleOutlined />}>Achieved</Tag>;
                  if (st === 'Behind') return <Tag color="red" icon={<ExclamationCircleOutlined />}>Behind</Tag>;
                  return <Tag color="processing" icon={<ClockCircleOutlined />}>In Progress</Tag>;
                }
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title="Add Performance Goal / OKR"
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
      >
        <Form form={form} layout="vertical" onFinish={handleGoalSubmit} initialValues={{ quarter: 'Q1', year: 2026, targetValue: 100, currentValue: 0, unit: 'Percentage' }}>
          <Form.Item name="title" label="Goal Title" rules={[{ required: true }]}>
            <Input placeholder="e.g. Achieve >98% SLA Compliance" />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea placeholder="Key result targets and milestones..." />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="quarter" label="Quarter">
                <Select options={[{ value: 'Q1' }, { value: 'Q2' }, { value: 'Q3' }, { value: 'Q4' }, { value: 'Annual' }]} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="year" label="Year">
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="currentValue" label="Current Progress">
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="targetValue" label="Target Value">
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit">Save Goal</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

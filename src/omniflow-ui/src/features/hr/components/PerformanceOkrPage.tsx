import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Progress, Button, Modal, Form, Input, Select, InputNumber, Row, Col, Statistic, message, Spin, Space, Popconfirm } from 'antd';
import { AimOutlined, PlusOutlined, CheckCircleOutlined, ClockCircleOutlined, ExclamationCircleOutlined, EditOutlined, DeleteOutlined, PercentageOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

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
  const { t } = useTranslation();
  const [goals, setGoals] = useState<EmployeeGoal[]>([]);
  const [loading, setLoading] = useState(true);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isProgressModalOpen, setIsProgressModalOpen] = useState(false);
  const [editingGoal, setEditingGoal] = useState<EmployeeGoal | null>(null);

  const [form] = Form.useForm();
  const [progressForm] = Form.useForm();

  const fetchGoals = async () => {
    setLoading(true);
    try {
      const res = await api.get('/hr/goals');
      setGoals(res.data);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGoals();
  }, []);

  const handleOpenAddModal = () => {
    setEditingGoal(null);
    form.resetFields();
    form.setFieldsValue({ quarter: 'Q1', year: 2026, targetValue: 100, currentValue: 0, unit: 'Percentage' });
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (goal: EmployeeGoal) => {
    setEditingGoal(goal);
    form.setFieldsValue(goal);
    setIsModalOpen(true);
  };

  const handleOpenProgressModal = (goal: EmployeeGoal) => {
    setEditingGoal(goal);
    progressForm.setFieldsValue({ currentValue: goal.currentValue, status: goal.status });
    setIsProgressModalOpen(true);
  };

  const handleGoalSubmit = async (values: any) => {
    try {
      const payload = { ...values, id: editingGoal?.id };
      await api.post('/hr/goals', payload);
      message.success(editingGoal ? t('okr.updated', 'Goal updated.') : t('okr.created', 'Goal created.'));
      setIsModalOpen(false);
      fetchGoals();
    } catch {
      message.error(t('okr.saveFailed', 'Failed to save goal.'));
    }
  };

  const handleProgressSubmit = async (values: any) => {
    if (!editingGoal) return;
    try {
      const payload = { ...editingGoal, ...values };
      await api.post('/hr/goals', payload);
      message.success(t('okr.progressUpdated', 'Progress updated.'));
      setIsProgressModalOpen(false);
      fetchGoals();
    } catch {
      message.error(t('okr.progressFailed', 'Failed to update progress.'));
    }
  };

  const handleDeleteGoal = async (id: string) => {
    try {
      await api.delete(`/hr/goals/${id}`);
      message.success(t('okr.deleted', 'Goal deleted.'));
      fetchGoals();
    } catch {
      message.error(t('okr.deleteFailed', 'Failed to delete goal.'));
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
              {t('okr.title', 'Performance & OKR Goal Tracking')}
            </h2>
            <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
              {t('okr.subtitle', 'Track quarterly (Q1-Q4) and annual Objectives & Key Results (OKRs).')}
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenAddModal}>
            {t('okr.addGoal', 'Add New Goal / OKR')}
          </Button>
        </div>

        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('okr.totalGoals', 'Total OKRs / Goals')} value={goals.length} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('okr.achievedGoals', 'Achieved Goals')} value={achievedCount} valueStyle={{ color: '#52c41a' }} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <div style={{ fontSize: 14, color: 'var(--text-secondary)', marginBottom: 4 }}>{t('okr.avgCompletion', 'Average OKR Completion')}</div>
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
                title: t('okr.colQuarter', 'Quarter / Year'),
                dataIndex: 'quarter',
                render: (q: string, record: EmployeeGoal) => (
                  <Tag color="blue">{q} {record.year}</Tag>
                )
              },
              { title: t('okr.colTitle', 'Goal Title'), dataIndex: 'title', render: (tt: string) => <strong>{tt}</strong> },
              { title: t('okr.colDescription', 'Description'), dataIndex: 'description' },
              {
                title: t('okr.colProgress', 'Progress'),
                dataIndex: 'progressPercent',
                render: (pct: number, record: EmployeeGoal) => (
                  <div style={{ width: 160 }}>
                    <Progress percent={pct} status={pct >= 100 ? 'success' : 'active'} />
                    <span style={{ fontSize: 11, color: 'var(--text-secondary)' }}>{record.currentValue} / {record.targetValue} {record.unit}</span>
                  </div>
                )
              },
              {
                title: t('okr.colStatus', 'Status'),
                dataIndex: 'status',
                render: (st: string) => {
                  if (st === 'Achieved') return <Tag color="green" icon={<CheckCircleOutlined />}>{t('okr.statusAchieved', 'Achieved')}</Tag>;
                  if (st === 'Behind') return <Tag color="red" icon={<ExclamationCircleOutlined />}>{t('okr.statusBehind', 'Behind')}</Tag>;
                  return <Tag color="processing" icon={<ClockCircleOutlined />}>{t('okr.statusInProgress', 'In Progress')}</Tag>;
                }
              },
              {
                title: t('okr.colActions', 'Actions'),
                key: 'actions',
                render: (_: any, record: EmployeeGoal) => (
                  <Space size="small">
                    <Button type="text" style={{ color: '#52c41a' }} icon={<PercentageOutlined />} onClick={() => handleOpenProgressModal(record)} title={t('okr.updateProgress', 'Update Progress')} />
                    <Button type="text" style={{ color: '#1890ff' }} icon={<EditOutlined />} onClick={() => handleOpenEditModal(record)} title={t('okr.editGoal', 'Edit Goal')} />
                    <Popconfirm title={t('okr.confirmDelete', 'Delete this goal?')} onConfirm={() => handleDeleteGoal(record.id)} okText={t('common.delete', 'Delete')} cancelText={t('common.cancel', 'Cancel')}>
                      <Button type="text" danger icon={<DeleteOutlined />} title={t('okr.deleteGoal', 'Delete Goal')} />
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title={editingGoal ? t('okr.editGoal', 'Edit Goal') : t('okr.addGoal', 'Add New Goal / OKR')}
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleGoalSubmit}>
          <Form.Item name="title" label={t('okr.colTitle', 'Goal Title')} rules={[{ required: true }]}>
            <Input placeholder={t('okr.titlePlaceholder', 'e.g. Achieve >98% SLA Compliance')} />
          </Form.Item>
          <Form.Item name="description" label={t('okr.colDescription', 'Description')}>
            <Input.TextArea placeholder={t('okr.descPlaceholder', 'Key result targets and milestones...')} />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="quarter" label={t('okr.quarter', 'Quarter')}>
                <Select options={[{ value: 'Q1' }, { value: 'Q2' }, { value: 'Q3' }, { value: 'Q4' }, { value: 'Annual' }]} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="year" label={t('okr.year', 'Year')}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="currentValue" label={t('okr.current', 'Current')}>
                <InputNumber style={{ width: '100%' }} disabled={!editingGoal} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="targetValue" label={t('okr.target', 'Target')}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="unit" label={t('okr.unit', 'Unit')}>
                <Select options={[{ value: 'Percentage' }, { value: 'Number' }, { value: 'Currency' }]} />
              </Form.Item>
            </Col>
          </Row>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit">{t('okr.saveGoal', 'Save Goal')}</Button>
          </div>
        </Form>
      </Modal>

      <Modal
        title={t('okr.updateProgress', 'Update Progress')}
        open={isProgressModalOpen}
        onCancel={() => setIsProgressModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={progressForm} layout="vertical" onFinish={handleProgressSubmit}>
          <div style={{ marginBottom: 16 }}>
            <strong>{editingGoal?.title}</strong>
            <div style={{ color: 'var(--text-secondary)' }}>{t('okr.target', 'Target')}: {editingGoal?.targetValue} {editingGoal?.unit}</div>
          </div>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="currentValue" label={t('okr.currentValue', 'Current Value')} rules={[{ required: true }]}>
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="status" label={t('okr.colStatus', 'Status')}>
                <Select options={[
                  { value: 'NotStarted', label: t('okr.statusNotStarted', 'Not Started') },
                  { value: 'InProgress', label: t('okr.statusInProgress', 'In Progress') },
                  { value: 'Behind', label: t('okr.statusBehindSchedule', 'Behind Schedule') },
                  { value: 'Achieved', label: t('okr.statusAchieved', 'Achieved') }
                ]} />
              </Form.Item>
            </Col>
          </Row>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsProgressModalOpen(false)} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit">{t('okr.updateProgress', 'Update Progress')}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

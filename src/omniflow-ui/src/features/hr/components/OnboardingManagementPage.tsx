import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Checkbox, Progress, Select, Row, Col, Statistic, message, Spin } from 'antd';
import { UserAddOutlined, UserDeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

interface OnboardingTask {
  id: string;
  employeeUserId: string;
  employeeName: string;
  type: 'Onboarding' | 'Offboarding';
  taskName: string;
  description: string;
  assignedDepartment: string;
  isCompleted: boolean;
  completedAt?: string;
  completedByName?: string;
  dueDate?: string;
}

export const OnboardingManagementPage: React.FC = () => {
  const { t } = useTranslation();
  const [tasks, setTasks] = useState<OnboardingTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [deptFilter, setDeptFilter] = useState<string>('All');

  const fetchTasks = async () => {
    setLoading(true);
    try {
      const res = await api.get('/onboarding/checklist');
      setTasks(res.data);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
  }, []);

  const handleTaskToggle = async (taskId: string, currentStatus: boolean) => {
    try {
      await api.post(`/onboarding/tasks/${taskId}/complete`, {
        isCompleted: !currentStatus
      });
      message.success(t('onb.taskUpdated', 'Task status updated.'));
      fetchTasks();
    } catch {
      message.error(t('onb.taskFailed', 'Failed to update task.'));
    }
  };

  const filteredTasks = deptFilter === 'All'
    ? tasks
    : tasks.filter(t => t.assignedDepartment.toUpperCase() === deptFilter.toUpperCase());

  const completedCount = tasks.filter(t => t.isCompleted).length;
  const totalCount = tasks.length || 1;
  const overallProgress = Math.round((completedCount / totalCount) * 100);

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <UserAddOutlined style={{ marginRight: 8, color: '#52c41a' }} />
              {t('onb.title', 'Onboarding & Offboarding Checklist Management')}
            </h2>
            <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
              {t('onb.subtitle', 'Track automated cross-departmental checklist tasks (IT, HR, Facilities, Finance).')}
            </div>
          </div>
          <Select
            value={deptFilter}
            onChange={setDeptFilter}
            options={[
              { value: 'All', label: t('onb.allDepartments', 'All Departments') },
              { value: 'IT', label: t('onb.itDepartment', 'IT Department') },
              { value: 'HR', label: t('onb.hrDepartment', 'HR Department') },
              { value: 'Facilities', label: t('onb.facilities', 'Facilities') },
              { value: 'Finance', label: t('onb.finance', 'Finance') }
            ]}
            style={{ width: 200 }}
          />
        </div>

        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('onb.totalTasks', 'Total Checklist Tasks')} value={tasks.length} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('onb.completedTasks', 'Completed Tasks')} value={completedCount} valueStyle={{ color: '#52c41a' }} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <div style={{ fontSize: 14, color: 'var(--text-secondary)', marginBottom: 4 }}>{t('onb.overallCompletion', 'Overall Workflow Completion')}</div>
              <Progress percent={overallProgress} status="active" />
            </Card>
          </Col>
        </Row>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={filteredTasks}
            rowKey="id"
            columns={[
              {
                title: t('onb.colStatus', 'Status'),
                dataIndex: 'isCompleted',
                render: (val: boolean, record: OnboardingTask) => (
                  <Checkbox
                    checked={val}
                    onChange={() => handleTaskToggle(record.id, val)}
                  >
                    {val ? <Tag color="green">{t('onb.done', 'Done')}</Tag> : <Tag color="orange">{t('onb.pending', 'Pending')}</Tag>}
                  </Checkbox>
                )
              },
              {
                title: t('onb.colWorkflow', 'Workflow'),
                dataIndex: 'type',
                render: (type: string) => (
                  <Tag color={type === 'Onboarding' ? 'blue' : 'magenta'} icon={type === 'Onboarding' ? <UserAddOutlined /> : <UserDeleteOutlined />}>
                    {type === 'Onboarding' ? t('onb.onboarding', 'Onboarding') : t('onb.offboarding', 'Offboarding')}
                  </Tag>
                )
              },
              { title: t('onb.colEmployee', 'Employee'), dataIndex: 'employeeName' },
              { title: t('onb.colTask', 'Task Action'), dataIndex: 'taskName' },
              {
                title: t('onb.colDept', 'Assigned Dept'),
                dataIndex: 'assignedDepartment',
                render: (dept: string) => <Tag color="purple">{dept}</Tag>
              },
              {
                title: t('onb.colDueDate', 'Due Date'),
                dataIndex: 'dueDate',
                render: (d: string) => d ? dayjs(d).format('DD MMM YYYY') : '-'
              },
              {
                title: t('onb.colCompletedBy', 'Completed By'),
                dataIndex: 'completedByName',
                render: (name: string, record: OnboardingTask) => record.isCompleted ? (name || t('onb.systemAuto', 'System Auto')) : '-'
              }
            ]}
          />
        )}
      </Card>
    </div>
  );
};

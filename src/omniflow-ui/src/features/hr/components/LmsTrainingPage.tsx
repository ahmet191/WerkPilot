import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Row, Col, Statistic, message, Spin } from 'antd';
import { ReadOutlined, CheckCircleOutlined, PlayCircleOutlined, FieldTimeOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';

interface TrainingCourse {
  id: string;
  employeeUserId: string;
  title: string;
  description: string;
  category: string;
  durationHours: number;
  isMandatory: boolean;
  status: 'NotStarted' | 'InProgress' | 'Completed';
  completedAt?: string;
  score: number;
}

export const LmsTrainingPage: React.FC = () => {
  const [courses, setCourses] = useState<TrainingCourse[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchCourses = async () => {
    setLoading(true);
    try {
      const res = await api.get('/hr/trainings');
      setCourses(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCourses();
  }, []);

  const handleCompleteCourse = async (courseId: string) => {
    try {
      await api.post(`/hr/trainings/${courseId}/complete`, { score: 98 });
      message.success('Training completed successfully!');
      fetchCourses();
    } catch (err) {
      message.error('Failed to complete training.');
    }
  };

  const completedCount = courses.filter(c => c.status === 'Completed').length;
  const mandatoryCount = courses.filter(c => c.isMandatory).length;

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <ReadOutlined style={{ marginRight: 8, color: '#722ed1' }} />
              LMS Corporate Training & Learning Management
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Mandatory compliance training and professional skill development courses.
            </div>
          </div>
        </div>

        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title="Assigned Courses" value={courses.length} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title="Mandatory Compliance" value={mandatoryCount} valueStyle={{ color: '#fa8c16' }} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title="Completed Courses" value={completedCount} valueStyle={{ color: '#52c41a' }} />
            </Card>
          </Col>
        </Row>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={courses}
            rowKey="id"
            columns={[
              {
                title: 'Category',
                dataIndex: 'category',
                render: (cat: string, record: TrainingCourse) => (
                  <Tag color={record.isMandatory ? 'red' : 'blue'}>
                    {cat} {record.isMandatory ? '(Mandatory)' : ''}
                  </Tag>
                )
              },
              { title: 'Course Title', dataIndex: 'title', render: (t: string) => <strong>{t}</strong> },
              { title: 'Description', dataIndex: 'description' },
              {
                title: 'Duration',
                dataIndex: 'durationHours',
                render: (hrs: number) => <Tag icon={<FieldTimeOutlined />}>{hrs} hours</Tag>
              },
              {
                title: 'Status',
                dataIndex: 'status',
                render: (st: string, record: TrainingCourse) => {
                  if (st === 'Completed') return <Tag color="green" icon={<CheckCircleOutlined />}>Completed ({record.score}%)</Tag>;
                  if (st === 'InProgress') return <Tag color="processing">In Progress</Tag>;
                  return <Tag color="default">Not Started</Tag>;
                }
              },
              {
                title: 'Action',
                key: 'action',
                render: (_: any, record: TrainingCourse) => (
                  record.status === 'Completed' ? (
                    <Button disabled icon={<CheckCircleOutlined />}>Done</Button>
                  ) : (
                    <Button type="primary" icon={<PlayCircleOutlined />} onClick={() => handleCompleteCourse(record.id)}>
                      Start Course
                    </Button>
                  )
                )
              }
            ]}
          />
        )}
      </Card>
    </div>
  );
};

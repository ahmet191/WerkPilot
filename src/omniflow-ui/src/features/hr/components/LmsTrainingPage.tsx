import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Row, Col, Statistic, message, Spin, Modal, Typography } from 'antd';
import { ReadOutlined, CheckCircleOutlined, PlayCircleOutlined, FieldTimeOutlined, VideoCameraOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';

const { Title, Paragraph, Text } = Typography;

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
  const [selectedCourse, setSelectedCourse] = useState<TrainingCourse | null>(null);
  const [isCourseModalOpen, setIsCourseModalOpen] = useState(false);

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

  const handleOpenCourse = (course: TrainingCourse) => {
    setSelectedCourse(course);
    setIsCourseModalOpen(true);
  };

  const handleCompleteCourse = async () => {
    if (!selectedCourse) return;
    try {
      // Generate a realistic random score between 85 and 100
      const realisticScore = Math.floor(Math.random() * 16) + 85; 
      await api.post(`/hr/trainings/${selectedCourse.id}/complete`, { score: realisticScore });
      message.success(`Training completed successfully with a score of ${realisticScore}!`);
      setIsCourseModalOpen(false);
      setSelectedCourse(null);
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
                    <Button type="primary" icon={<PlayCircleOutlined />} onClick={() => handleOpenCourse(record)}>
                      Start Course
                    </Button>
                  )
                )
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title={null}
        open={isCourseModalOpen}
        onCancel={() => setIsCourseModalOpen(false)}
        footer={null}
        width={800}
        destroyOnClose
      >
        {selectedCourse && (
          <div>
            <div style={{ background: '#000', height: 400, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 24, marginTop: 16 }}>
              <div style={{ textAlign: 'center', color: '#fff' }}>
                <VideoCameraOutlined style={{ fontSize: 64, opacity: 0.5, marginBottom: 16 }} />
                <h3>Video Playback Simulation</h3>
                <p style={{ opacity: 0.7 }}>In a real implementation, a video player (e.g., YouTube/Vimeo embed or internal LMS player) would be here.</p>
              </div>
            </div>
            
            <Title level={3}>{selectedCourse.title}</Title>
            <div style={{ display: 'flex', gap: 12, marginBottom: 16 }}>
               <Tag color="blue">{selectedCourse.category}</Tag>
               <Tag icon={<FieldTimeOutlined />}>{selectedCourse.durationHours} hours</Tag>
               {selectedCourse.isMandatory && <Tag color="red">Mandatory</Tag>}
            </div>
            <Paragraph style={{ fontSize: 16, color: '#595959' }}>
              {selectedCourse.description}
            </Paragraph>
            
            <div style={{ background: '#f5f5f5', padding: 16, borderRadius: 8, marginBottom: 24 }}>
              <Text strong>Course Objectives:</Text>
              <ul style={{ marginTop: 8, paddingLeft: 20 }}>
                <li>Understand the core principles outlined in the training.</li>
                <li>Apply the knowledge to your daily workflow and company policies.</li>
                <li>Complete the final assessment to receive credit for this module.</li>
              </ul>
            </div>

            <div style={{ textAlign: 'right', borderTop: '1px solid #f0f0f0', paddingTop: 16 }}>
              <Button onClick={() => setIsCourseModalOpen(false)} style={{ marginRight: 12 }}>Save for Later</Button>
              <Button type="primary" icon={<CheckCircleOutlined />} onClick={handleCompleteCourse} size="large">
                Mark as Completed & Take Exam
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

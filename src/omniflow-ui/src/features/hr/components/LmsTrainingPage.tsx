import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Row, Col, Statistic, message, Spin, Modal, Typography, InputNumber } from 'antd';
import { ReadOutlined, CheckCircleOutlined, PlayCircleOutlined, FieldTimeOutlined, VideoCameraOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

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
  const { t } = useTranslation();
  const [courses, setCourses] = useState<TrainingCourse[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedCourse, setSelectedCourse] = useState<TrainingCourse | null>(null);
  const [isCourseModalOpen, setIsCourseModalOpen] = useState(false);
  const [examScore, setExamScore] = useState<number>(100);

  const fetchCourses = async () => {
    setLoading(true);
    try {
      const res = await api.get('/hr/trainings');
      setCourses(res.data);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCourses();
  }, []);

  const handleOpenCourse = (course: TrainingCourse) => {
    setSelectedCourse(course);
    setExamScore(course.score > 0 ? course.score : 100);
    setIsCourseModalOpen(true);
  };

  const handleCompleteCourse = async () => {
    if (!selectedCourse) return;
    try {
      await api.post(`/hr/trainings/${selectedCourse.id}/complete`, { score: examScore });
      message.success(t('lms.completed', 'Training completed with an assessment score of {n}.').replace('{n}', String(examScore)));
      setIsCourseModalOpen(false);
      setSelectedCourse(null);
      fetchCourses();
    } catch {
      message.error(t('lms.completeFailed', 'Failed to complete training.'));
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
              {t('lms.title', 'LMS Corporate Training & Learning Management')}
            </h2>
            <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
              {t('lms.subtitle', 'Mandatory compliance training and professional skill development courses.')}
            </div>
          </div>
        </div>

        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('lms.assignedCourses', 'Assigned Courses')} value={courses.length} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('lms.mandatoryCompliance', 'Mandatory Compliance')} value={mandatoryCount} valueStyle={{ color: '#fa8c16' }} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card size="small">
              <Statistic title={t('lms.completedCourses', 'Completed Courses')} value={completedCount} valueStyle={{ color: '#52c41a' }} />
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
                title: t('lms.colCategory', 'Category'),
                dataIndex: 'category',
                render: (cat: string, record: TrainingCourse) => (
                  <Tag color={record.isMandatory ? 'red' : 'blue'}>
                    {cat} {record.isMandatory ? t('lms.mandatorySuffix', '(Mandatory)') : ''}
                  </Tag>
                )
              },
              { title: t('lms.colTitle', 'Course Title'), dataIndex: 'title', render: (tt: string) => <strong>{tt}</strong> },
              { title: t('lms.colDescription', 'Description'), dataIndex: 'description' },
              {
                title: t('lms.colDuration', 'Duration'),
                dataIndex: 'durationHours',
                render: (hrs: number) => <Tag icon={<FieldTimeOutlined />}>{hrs} {t('lms.hours', 'hours')}</Tag>
              },
              {
                title: t('lms.colStatus', 'Status'),
                dataIndex: 'status',
                render: (st: string, record: TrainingCourse) => {
                  if (st === 'Completed') return <Tag color="green" icon={<CheckCircleOutlined />}>{t('lms.statusCompleted', 'Completed')} ({record.score}%)</Tag>;
                  if (st === 'InProgress') return <Tag color="processing">{t('lms.statusInProgress', 'In Progress')}</Tag>;
                  return <Tag color="default">{t('lms.statusNotStarted', 'Not Started')}</Tag>;
                }
              },
              {
                title: t('lms.colAction', 'Action'),
                key: 'action',
                render: (_: any, record: TrainingCourse) => (
                  record.status === 'Completed' ? (
                    <Button disabled icon={<CheckCircleOutlined />}>{t('lms.done', 'Done')}</Button>
                  ) : (
                    <Button type="primary" icon={<PlayCircleOutlined />} onClick={() => handleOpenCourse(record)}>
                      {t('lms.startCourse', 'Start Course')}
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
                <h3>{t('lms.videoSim', 'Video Playback Simulation')}</h3>
                <p style={{ opacity: 0.7 }}>{t('lms.videoSimNote', 'In a real implementation, a video player (e.g., YouTube/Vimeo embed or internal LMS player) would be here.')}</p>
              </div>
            </div>
            
            <Title level={3}>{selectedCourse.title}</Title>
            <div style={{ display: 'flex', gap: 12, marginBottom: 16 }}>
               <Tag color="blue">{selectedCourse.category}</Tag>
               <Tag icon={<FieldTimeOutlined />}>{selectedCourse.durationHours} hours</Tag>
               {selectedCourse.isMandatory && <Tag color="red">Mandatory</Tag>}
            </div>
            <Paragraph style={{ fontSize: 16, color: 'var(--text-secondary)' }}>
              {selectedCourse.description}
            </Paragraph>
            
            <div style={{ background: 'var(--bg-elevated)', padding: 16, borderRadius: 8, marginBottom: 24 }}>
              <Text strong>{t('lms.objectives', 'Course Objectives:')}</Text>
              <ul style={{ marginTop: 8, paddingLeft: 20 }}>
                <li>{t('lms.objective1', 'Understand the core principles outlined in the training.')}</li>
                <li>{t('lms.objective2', 'Apply the knowledge to your daily workflow and company policies.')}</li>
                <li>{t('lms.objective3', 'Complete the final assessment to receive credit for this module.')}</li>
              </ul>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 12, borderTop: '1px solid var(--border-color)', paddingTop: 16 }}>
              <Text type="secondary">{t('lms.assessmentScore', 'Assessment score:')}</Text>
              <InputNumber min={0} max={100} value={examScore} onChange={(v) => setExamScore(v ?? 0)} style={{ width: 90 }} />
              <Button onClick={() => setIsCourseModalOpen(false)}>{t('lms.saveForLater', 'Save for Later')}</Button>
              <Button type="primary" icon={<CheckCircleOutlined />} onClick={handleCompleteCourse} size="large">
                {t('lms.recordScore', 'Complete & Record Score')}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Rate, Alert, Row, Col, Spin, Divider, Button, Modal, Form, Input, Select, DatePicker, Checkbox, message } from 'antd';
import { TrophyOutlined, ExclamationCircleOutlined, CheckCircleOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api } from '../../../core/api';

interface SkillItem {
  id: string;
  skillName: string;
  category: string;
  proficiencyLevel: number;
  lastEvaluatedAt: string;
}

interface CertificateItem {
  id: string;
  certificateName: string;
  issuingOrganization: string;
  credentialId: string;
  issueDate: string;
  expirationDate?: string;
  isMandatory: boolean;
  isExpired: boolean;
  isExpiringSoon: boolean;
}

export const SkillsCertificatesPage: React.FC = () => {
  const [skills, setSkills] = useState<SkillItem[]>([]);
  const [certificates, setCertificates] = useState<CertificateItem[]>([]);
  const [loading, setLoading] = useState(true);

  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [isCertModalOpen, setIsCertModalOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [skillForm] = Form.useForm();
  const [certForm] = Form.useForm();

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/hr/skills-certificates');
      setSkills(res.data.skills);
      setCertificates(res.data.certificates);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleAddSkill = async (values: any) => {
    setSubmitting(true);
    try {
      await api.post('/hr/skills', values);
      message.success('Skill added successfully!');
      setIsSkillModalOpen(false);
      skillForm.resetFields();
      fetchData();
    } catch {
      message.error('Failed to add skill.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleAddCertificate = async (values: any) => {
    setSubmitting(true);
    try {
      const payload = {
        ...values,
        issueDate: values.issueDate.toISOString(),
        expirationDate: values.expirationDate ? values.expirationDate.toISOString() : null,
      };
      await api.post('/hr/certificates', payload);
      message.success('Certificate added successfully!');
      setIsCertModalOpen(false);
      certForm.resetFields();
      fetchData();
    } catch {
      message.error('Failed to add certificate.');
    } finally {
      setSubmitting(false);
    }
  };

  const expiringAlerts = certificates.filter(c => c.isExpiringSoon || c.isExpired);

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <h2 style={{ margin: '0 0 16px 0', fontWeight: 700 }}>
          <TrophyOutlined style={{ marginRight: 8, color: '#fa8c16' }} />
          Competency Skill Matrix & Certification Vault
        </h2>

        {expiringAlerts.length > 0 && (
          <Alert
            message="Certification Renewal Warning"
            description={`${expiringAlerts.length} certification(s) require immediate renewal before expiry.`}
            type="warning"
            showIcon
            icon={<ExclamationCircleOutlined />}
            style={{ marginBottom: 24 }}
          />
        )}

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>🎯 Skill Matrix & Competency Evaluations</h3>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsSkillModalOpen(true)}>
                Add Skill
              </Button>
            </div>
            
            <Row gutter={[16, 16]} style={{ marginBottom: 32 }}>
              {skills.map((s) => (
                <Col xs={24} sm={12} md={8} key={s.id}>
                  <Card size="small" style={{ borderRadius: 12, border: '1px solid #e8e8e8' }}>
                    <Tag color="cyan" style={{ marginBottom: 8 }}>{s.category}</Tag>
                    <h4 style={{ margin: 0 }}>{s.skillName}</h4>
                    <div style={{ marginTop: 8 }}>
                      <Rate disabled defaultValue={s.proficiencyLevel} count={5} />
                    </div>
                  </Card>
                </Col>
              ))}
              {skills.length === 0 && (
                <Col span={24}>
                  <div style={{ textAlign: 'center', color: '#8c8c8c', padding: 20 }}>No skills added yet.</div>
                </Col>
              )}
            </Row>

            <Divider />

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>📜 Certification Vault & Expiry Alarms</h3>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsCertModalOpen(true)}>
                Add Certificate
              </Button>
            </div>

            <Table
              dataSource={certificates}
              rowKey="id"
              locale={{ emptyText: 'No certificates added yet.' }}
              columns={[
                {
                  title: 'Certification Name',
                  dataIndex: 'certificateName',
                  render: (name: string, record: CertificateItem) => (
                    <div>
                      <strong>{name}</strong>
                      {record.isMandatory && <Tag color="red" style={{ marginLeft: 8 }}>Mandatory</Tag>}
                    </div>
                  )
                },
                { title: 'Issuing Body', dataIndex: 'issuingOrganization' },
                { title: 'Credential ID', dataIndex: 'credentialId' },
                {
                  title: 'Issue Date',
                  dataIndex: 'issueDate',
                  render: (d: string) => dayjs(d).format('DD MMM YYYY')
                },
                {
                  title: 'Expiration Status',
                  dataIndex: 'expirationDate',
                  render: (exp: string, record: CertificateItem) => {
                    if (!exp) return <Tag color="default">No Expiry</Tag>;
                    if (record.isExpired) return <Tag color="red" icon={<ExclamationCircleOutlined />}>Expired ({dayjs(exp).format('DD/MM/YYYY')})</Tag>;
                    if (record.isExpiringSoon) return <Tag color="warning" icon={<ExclamationCircleOutlined />}>Expires Soon ({dayjs(exp).format('DD/MM/YYYY')})</Tag>;
                    return <Tag color="green" icon={<CheckCircleOutlined />}>Valid until {dayjs(exp).format('DD/MM/YYYY')}</Tag>;
                  }
                }
              ]}
            />
          </div>
        )}
      </Card>

      {/* Add Skill Modal */}
      <Modal
        title="Add New Skill"
        open={isSkillModalOpen}
        onCancel={() => setIsSkillModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={skillForm} layout="vertical" onFinish={handleAddSkill}>
          <Form.Item name="skillName" label="Skill Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. React, C#, Leadership" />
          </Form.Item>
          <Form.Item name="category" label="Category" rules={[{ required: true }]}>
            <Select options={[
              { value: 'Technical', label: 'Technical' },
              { value: 'Leadership', label: 'Leadership' },
              { value: 'Domain', label: 'Domain' },
              { value: 'Compliance', label: 'Compliance' }
            ]} />
          </Form.Item>
          <Form.Item name="proficiencyLevel" label="Proficiency Level (1-5)" rules={[{ required: true }]}>
            <Rate count={5} />
          </Form.Item>
          <div style={{ textAlign: 'right' }}>
            <Button onClick={() => setIsSkillModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>Add Skill</Button>
          </div>
        </Form>
      </Modal>

      {/* Add Certificate Modal */}
      <Modal
        title="Add Certification"
        open={isCertModalOpen}
        onCancel={() => setIsCertModalOpen(false)}
        footer={null}
        destroyOnClose
      >
        <Form form={certForm} layout="vertical" onFinish={handleAddCertificate}>
          <Form.Item name="certificateName" label="Certification Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. AWS Certified Solutions Architect" />
          </Form.Item>
          <Form.Item name="issuingOrganization" label="Issuing Organization" rules={[{ required: true }]}>
            <Input placeholder="e.g. Amazon Web Services" />
          </Form.Item>
          <Form.Item name="credentialId" label="Credential ID">
            <Input />
          </Form.Item>
          <Form.Item name="issueDate" label="Issue Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="expirationDate" label="Expiration Date (Optional)">
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="isMandatory" valuePropName="checked">
            <Checkbox>This certification is legally mandatory for my role</Checkbox>
          </Form.Item>
          <div style={{ textAlign: 'right' }}>
            <Button onClick={() => setIsCertModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>Add Certificate</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Rate, Alert, Row, Col, Spin, Divider, Button, Modal, Form, Input, Select, DatePicker, Checkbox, Popconfirm, Space, message } from 'antd';
import { TrophyOutlined, ExclamationCircleOutlined, CheckCircleOutlined, PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
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
  const [editingSkillId, setEditingSkillId] = useState<string | null>(null);
  const [editingCertId, setEditingCertId] = useState<string | null>(null);
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

  const openSkillModal = (skill?: SkillItem) => {
    if (skill) {
      setEditingSkillId(skill.id);
      skillForm.setFieldsValue({ skillName: skill.skillName, category: skill.category, proficiencyLevel: skill.proficiencyLevel });
    } else {
      setEditingSkillId(null);
      skillForm.resetFields();
    }
    setIsSkillModalOpen(true);
  };

  const handleSaveSkill = async (values: any) => {
    setSubmitting(true);
    try {
      await api.post('/hr/skills', editingSkillId ? { ...values, id: editingSkillId } : values);
      message.success(editingSkillId ? 'Skill updated.' : 'Skill added.');
      setIsSkillModalOpen(false);
      setEditingSkillId(null);
      skillForm.resetFields();
      fetchData();
    } catch {
      message.error('Failed to save skill.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteSkill = async (id: string) => {
    try {
      await api.delete(`/hr/skills/${id}`);
      message.success('Skill removed.');
      fetchData();
    } catch {
      message.error('Failed to remove skill.');
    }
  };

  const openCertModal = (cert?: CertificateItem) => {
    if (cert) {
      setEditingCertId(cert.id);
      certForm.setFieldsValue({
        certificateName: cert.certificateName,
        issuingOrganization: cert.issuingOrganization,
        credentialId: cert.credentialId,
        issueDate: cert.issueDate ? dayjs(cert.issueDate) : undefined,
        expirationDate: cert.expirationDate ? dayjs(cert.expirationDate) : undefined,
        isMandatory: cert.isMandatory,
      });
    } else {
      setEditingCertId(null);
      certForm.resetFields();
    }
    setIsCertModalOpen(true);
  };

  const handleSaveCertificate = async (values: any) => {
    setSubmitting(true);
    try {
      const payload = {
        ...values,
        id: editingCertId ?? undefined,
        issueDate: values.issueDate.toISOString(),
        expirationDate: values.expirationDate ? values.expirationDate.toISOString() : null,
      };
      await api.post('/hr/certificates', payload);
      message.success(editingCertId ? 'Certificate updated.' : 'Certificate added.');
      setIsCertModalOpen(false);
      setEditingCertId(null);
      certForm.resetFields();
      fetchData();
    } catch {
      message.error('Failed to save certificate.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteCertificate = async (id: string) => {
    try {
      await api.delete(`/hr/certificates/${id}`);
      message.success('Certificate removed.');
      fetchData();
    } catch {
      message.error('Failed to remove certificate.');
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
              <Button type="primary" icon={<PlusOutlined />} onClick={() => openSkillModal()}>
                Add Skill
              </Button>
            </div>

            <Row gutter={[16, 16]} style={{ marginBottom: 32 }}>
              {skills.map((s) => (
                <Col xs={24} sm={12} md={8} key={s.id}>
                  <Card size="small" style={{ borderRadius: 12, border: '1px solid #e8e8e8' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <Tag color="cyan" style={{ marginBottom: 8 }}>{s.category}</Tag>
                      <Space size={0}>
                        <Button type="text" size="small" icon={<EditOutlined />} onClick={() => openSkillModal(s)} />
                        <Popconfirm title="Remove this skill?" onConfirm={() => handleDeleteSkill(s.id)} okText="Remove" cancelText="Cancel">
                          <Button type="text" size="small" danger icon={<DeleteOutlined />} />
                        </Popconfirm>
                      </Space>
                    </div>
                    <h4 style={{ margin: 0 }}>{s.skillName}</h4>
                    <div style={{ marginTop: 8 }}>
                      <Rate disabled value={s.proficiencyLevel} count={5} />
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
              <Button type="primary" icon={<PlusOutlined />} onClick={() => openCertModal()}>
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
                },
                {
                  title: 'Actions',
                  key: 'actions',
                  width: 100,
                  render: (_: unknown, record: CertificateItem) => (
                    <Space size={0}>
                      <Button type="text" size="small" icon={<EditOutlined />} onClick={() => openCertModal(record)} />
                      <Popconfirm title="Remove this certificate?" onConfirm={() => handleDeleteCertificate(record.id)} okText="Remove" cancelText="Cancel">
                        <Button type="text" size="small" danger icon={<DeleteOutlined />} />
                      </Popconfirm>
                    </Space>
                  )
                }
              ]}
            />
          </div>
        )}
      </Card>

      {/* Add / Edit Skill Modal */}
      <Modal
        title={editingSkillId ? 'Edit Skill' : 'Add New Skill'}
        open={isSkillModalOpen}
        onCancel={() => { setIsSkillModalOpen(false); setEditingSkillId(null); }}
        footer={null}
        destroyOnClose
      >
        <Form form={skillForm} layout="vertical" onFinish={handleSaveSkill}>
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
            <Button onClick={() => { setIsSkillModalOpen(false); setEditingSkillId(null); }} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>{editingSkillId ? 'Save' : 'Add Skill'}</Button>
          </div>
        </Form>
      </Modal>

      {/* Add / Edit Certificate Modal */}
      <Modal
        title={editingCertId ? 'Edit Certification' : 'Add Certification'}
        open={isCertModalOpen}
        onCancel={() => { setIsCertModalOpen(false); setEditingCertId(null); }}
        footer={null}
        destroyOnClose
      >
        <Form form={certForm} layout="vertical" onFinish={handleSaveCertificate}>
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
            <Button onClick={() => { setIsCertModalOpen(false); setEditingCertId(null); }} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>{editingCertId ? 'Save' : 'Add Certificate'}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

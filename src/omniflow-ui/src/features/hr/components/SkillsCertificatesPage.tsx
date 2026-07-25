import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Rate, Alert, Row, Col, Spin, Divider, Button, Modal, Form, Input, Select, DatePicker, Checkbox, Popconfirm, Space, message } from 'antd';
import { TrophyOutlined, ExclamationCircleOutlined, CheckCircleOutlined, PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

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
  const { t } = useTranslation();
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
      if (editingSkillId) await api.put(`/hr/skills/${editingSkillId}`, values);
      else await api.post('/hr/skills', values);
      message.success(editingSkillId ? t('skills.skillUpdated', 'Skill updated.') : t('skills.skillAdded', 'Skill added.'));
      setIsSkillModalOpen(false);
      setEditingSkillId(null);
      skillForm.resetFields();
      fetchData();
    } catch {
      message.error(t('skills.skillSaveFailed', 'Failed to save skill.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteSkill = async (id: string) => {
    try {
      await api.delete(`/hr/skills/${id}`);
      message.success(t('skills.skillRemoved', 'Skill removed.'));
      fetchData();
    } catch {
      message.error(t('skills.skillRemoveFailed', 'Failed to remove skill.'));
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
        issueDate: values.issueDate.toISOString(),
        expirationDate: values.expirationDate ? values.expirationDate.toISOString() : null,
      };
      if (editingCertId) await api.put(`/hr/certificates/${editingCertId}`, payload);
      else await api.post('/hr/certificates', payload);
      message.success(editingCertId ? t('skills.certUpdated', 'Certificate updated.') : t('skills.certAdded', 'Certificate added.'));
      setIsCertModalOpen(false);
      setEditingCertId(null);
      certForm.resetFields();
      fetchData();
    } catch {
      message.error(t('skills.certSaveFailed', 'Failed to save certificate.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteCertificate = async (id: string) => {
    try {
      await api.delete(`/hr/certificates/${id}`);
      message.success(t('skills.certRemoved', 'Certificate removed.'));
      fetchData();
    } catch {
      message.error(t('skills.certRemoveFailed', 'Failed to remove certificate.'));
    }
  };

  const expiringAlerts = certificates.filter(c => c.isExpiringSoon || c.isExpired);

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <h2 style={{ margin: '0 0 16px 0', fontWeight: 700 }}>
          <TrophyOutlined style={{ marginRight: 8, color: '#fa8c16' }} />
          {t('skills.title', 'Competency Skill Matrix & Certification Vault')}
        </h2>

        {expiringAlerts.length > 0 && (
          <Alert
            message={t('skills.renewalWarning', 'Certification Renewal Warning')}
            description={t('skills.renewalDesc', '{n} certification(s) require immediate renewal before expiry.').replace('{n}', String(expiringAlerts.length))}
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
              <h3 style={{ margin: 0 }}>🎯 {t('skills.matrixSection', 'Skill Matrix & Competency Evaluations')}</h3>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => openSkillModal()}>
                {t('skills.addSkill', 'Add Skill')}
              </Button>
            </div>

            <Row gutter={[16, 16]} style={{ marginBottom: 32 }}>
              {skills.map((s) => (
                <Col xs={24} sm={12} md={8} key={s.id}>
                  <Card size="small" style={{ borderRadius: 12, border: '1px solid var(--border-color)' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <Tag color="cyan" style={{ marginBottom: 8 }}>{s.category}</Tag>
                      <Space size={0}>
                        <Button type="text" size="small" icon={<EditOutlined />} onClick={() => openSkillModal(s)} />
                        <Popconfirm title={t('skills.confirmRemoveSkill', 'Remove this skill?')} onConfirm={() => handleDeleteSkill(s.id)} okText={t('skills.remove', 'Remove')} cancelText={t('common.cancel', 'Cancel')}>
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
                  <div style={{ textAlign: 'center', color: 'var(--text-secondary)', padding: 20 }}>{t('skills.noSkills', 'No skills added yet.')}</div>
                </Col>
              )}
            </Row>

            <Divider />

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>📜 {t('skills.vaultSection', 'Certification Vault & Expiry Alarms')}</h3>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => openCertModal()}>
                {t('skills.addCert', 'Add Certificate')}
              </Button>
            </div>

            <Table
              dataSource={certificates}
              rowKey="id"
              locale={{ emptyText: t('skills.noCerts', 'No certificates added yet.') }}
              columns={[
                {
                  title: t('skills.colCertName', 'Certification Name'),
                  dataIndex: 'certificateName',
                  render: (name: string, record: CertificateItem) => (
                    <div>
                      <strong>{name}</strong>
                      {record.isMandatory && <Tag color="red" style={{ marginLeft: 8 }}>{t('skills.mandatory', 'Mandatory')}</Tag>}
                    </div>
                  )
                },
                { title: t('skills.colIssuer', 'Issuing Body'), dataIndex: 'issuingOrganization' },
                { title: t('skills.colCredential', 'Credential ID'), dataIndex: 'credentialId' },
                {
                  title: t('skills.colIssueDate', 'Issue Date'),
                  dataIndex: 'issueDate',
                  render: (d: string) => dayjs(d).format('DD MMM YYYY')
                },
                {
                  title: t('skills.colExpiry', 'Expiration Status'),
                  dataIndex: 'expirationDate',
                  render: (exp: string, record: CertificateItem) => {
                    if (!exp) return <Tag color="default">{t('skills.noExpiry', 'No Expiry')}</Tag>;
                    if (record.isExpired) return <Tag color="red" icon={<ExclamationCircleOutlined />}>{t('skills.expired', 'Expired')} ({dayjs(exp).format('DD/MM/YYYY')})</Tag>;
                    if (record.isExpiringSoon) return <Tag color="warning" icon={<ExclamationCircleOutlined />}>{t('skills.expiresSoon', 'Expires Soon')} ({dayjs(exp).format('DD/MM/YYYY')})</Tag>;
                    return <Tag color="green" icon={<CheckCircleOutlined />}>{t('skills.validUntil', 'Valid until')} {dayjs(exp).format('DD/MM/YYYY')}</Tag>;
                  }
                },
                {
                  title: t('skills.colActions', 'Actions'),
                  key: 'actions',
                  width: 100,
                  render: (_: unknown, record: CertificateItem) => (
                    <Space size={0}>
                      <Button type="text" size="small" icon={<EditOutlined />} onClick={() => openCertModal(record)} />
                      <Popconfirm title={t('skills.confirmRemoveCert', 'Remove this certificate?')} onConfirm={() => handleDeleteCertificate(record.id)} okText={t('skills.remove', 'Remove')} cancelText={t('common.cancel', 'Cancel')}>
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
        title={editingSkillId ? t('skills.editSkill', 'Edit Skill') : t('skills.addNewSkill', 'Add New Skill')}
        open={isSkillModalOpen}
        onCancel={() => { setIsSkillModalOpen(false); setEditingSkillId(null); }}
        footer={null}
        destroyOnClose
      >
        <Form form={skillForm} layout="vertical" onFinish={handleSaveSkill}>
          <Form.Item name="skillName" label={t('skills.skillName', 'Skill Name')} rules={[{ required: true }]}>
            <Input placeholder={t('skills.skillNamePlaceholder', 'e.g. React, C#, Leadership')} />
          </Form.Item>
          <Form.Item name="category" label={t('skills.category', 'Category')} rules={[{ required: true }]}>
            <Select options={[
              { value: 'Technical', label: t('skills.catTechnical', 'Technical') },
              { value: 'Leadership', label: t('skills.catLeadership', 'Leadership') },
              { value: 'Domain', label: t('skills.catDomain', 'Domain') },
              { value: 'Compliance', label: t('skills.catCompliance', 'Compliance') }
            ]} />
          </Form.Item>
          <Form.Item name="proficiencyLevel" label={t('skills.proficiency', 'Proficiency Level (1-5)')} rules={[{ required: true }]}>
            <Rate count={5} />
          </Form.Item>
          <div style={{ textAlign: 'right' }}>
            <Button onClick={() => { setIsSkillModalOpen(false); setEditingSkillId(null); }} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>{editingSkillId ? t('common.save', 'Save') : t('skills.addSkill', 'Add Skill')}</Button>
          </div>
        </Form>
      </Modal>

      {/* Add / Edit Certificate Modal */}
      <Modal
        title={editingCertId ? t('skills.editCert', 'Edit Certification') : t('skills.addCertification', 'Add Certification')}
        open={isCertModalOpen}
        onCancel={() => { setIsCertModalOpen(false); setEditingCertId(null); }}
        footer={null}
        destroyOnClose
      >
        <Form form={certForm} layout="vertical" onFinish={handleSaveCertificate}>
          <Form.Item name="certificateName" label={t('skills.colCertName', 'Certification Name')} rules={[{ required: true }]}>
            <Input placeholder={t('skills.certNamePlaceholder', 'e.g. AWS Certified Solutions Architect')} />
          </Form.Item>
          <Form.Item name="issuingOrganization" label={t('skills.issuingOrg', 'Issuing Organization')} rules={[{ required: true }]}>
            <Input placeholder={t('skills.issuerPlaceholder', 'e.g. Amazon Web Services')} />
          </Form.Item>
          <Form.Item name="credentialId" label={t('skills.colCredential', 'Credential ID')}>
            <Input />
          </Form.Item>
          <Form.Item name="issueDate" label={t('skills.colIssueDate', 'Issue Date')} rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="expirationDate" label={t('skills.expiryOptional', 'Expiration Date (Optional)')}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="isMandatory" valuePropName="checked">
            <Checkbox>{t('skills.mandatoryLabel', 'This certification is legally mandatory for my role')}</Checkbox>
          </Form.Item>
          <div style={{ textAlign: 'right' }}>
            <Button onClick={() => { setIsCertModalOpen(false); setEditingCertId(null); }} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>{editingCertId ? t('common.save', 'Save') : t('skills.addCert', 'Add Certificate')}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

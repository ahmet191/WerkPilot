import React, { useEffect, useState } from 'react';
import {
  Card,
  Tabs,
  Tag,
  Button,
  Form,
  Input,
  Select,
  DatePicker,
  Table,
  Upload,
  Progress,
  Row,
  Col,
  Statistic,
  message,
  Modal,
  Spin,
  Alert,
  Divider
} from 'antd';
import {
  UserOutlined,
  BankOutlined,
  FileTextOutlined,
  LaptopOutlined,
  CalendarOutlined,
  UploadOutlined,
  SafetyCertificateOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

interface GermanyDetails {
  taxId: string;
  socialSecurityNo: string;
  healthInsuranceProvider: string;
  taxClass: number;
}

interface TurkeyDetails {
  tcKimlikNo: string;
  sgkNo: string;
  militaryStatus: number;
  disabilityDegree: number;
}

interface AssetSummary {
  id: string;
  assetTag: string;
  model: string;
  category: string;
  status: string;
}

interface LeaveBalanceSummary {
  leaveType: string;
  totalEntitlementDays: number;
  usedDays: number;
  remainingDays: number;
}

interface HrProfile {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  departmentName: string;
  jobTitle: string;
  country: 'TR' | 'DE';
  birthDate?: string;
  gender?: string;
  bloodType?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  addressLine?: string;
  city?: string;
  postalCode?: string;
  hireDate: string;
  trialPeriodEndDate?: string;
  employmentType: string;
  workingHoursPerWeek: number;
  managerName?: string;
  grossSalary: number;
  currency: string;
  bankName: string;
  iban: string;
  bicSwift?: string;
  germanyDetails: GermanyDetails;
  turkeyDetails: TurkeyDetails;
  assignedAssets: AssetSummary[];
  leaveBalances: LeaveBalanceSummary[];
}

interface EmployeeDocument {
  id: string;
  category: string;
  title: string;
  fileName: string;
  fileSizeBytes: number;
  expirationDate?: string;
  uploadedAt: string;
  isExpired: boolean;
  isExpiringSoon: boolean;
}

export const EmployeeCenterPage: React.FC = () => {
  const { t } = useTranslation();
  const [profile, setProfile] = useState<HrProfile | null>(null);
  const [documents, setDocuments] = useState<EmployeeDocument[]>([]);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [uploading, setUploading] = useState(false);

  const [form] = Form.useForm();
  const [uploadForm] = Form.useForm();

  const fetchHrData = React.useCallback(async () => {
    setLoading(true);
    try {
      const [profileRes, docsRes] = await Promise.all([
        api.get('/hr/my-profile'),
        api.get('/hr/my-documents')
      ]);
      setProfile(profileRes.data);
      setDocuments(docsRes.data);
      form.setFieldsValue({
        ...profileRes.data,
        birthDate: profileRes.data.birthDate ? dayjs(profileRes.data.birthDate) : null
      });
    } catch {
      message.error(t('form.error', 'Failed to load HR Profile.'));
    } finally {
      setLoading(false);
    }
  }, [form, t]);

  useEffect(() => {
    fetchHrData();
  }, [fetchHrData]);

  const handleProfileSave = async (values: any) => {
    try {
      const payload = {
        ...values,
        birthDate: values.birthDate ? values.birthDate.toISOString() : null
      };
      await api.put('/hr/my-profile', payload);
      message.success(t('form.success', 'Profile updated successfully!'));
      setIsEditing(false);
      fetchHrData();
    } catch {
      message.error(t('form.error', 'Error saving profile.'));
    }
  };

  const handleDocumentUpload = async (values: any) => {
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('category', values.category);
      formData.append('title', values.title || '');
      if (values.expirationDate) {
        formData.append('expirationDate', values.expirationDate.toISOString());
      }
      formData.append('file', values.file.file.originFileObj);

      await api.post('/hr/my-documents', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      message.success(t('form.success', 'Document uploaded successfully!'));
      setIsUploadModalOpen(false);
      uploadForm.resetFields();
      fetchHrData();
    } catch {
      message.error(t('form.error', 'Document upload failed.'));
    } finally {
      setUploading(false);
    }
  };

  if (loading || !profile) {
    return (
      <div style={{ padding: 40, textAlign: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  const isGermany = profile.country === 'DE';

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      {/* Header Banner */}
      <Card
        style={{
          marginBottom: 24,
          background: 'linear-gradient(135deg, rgba(24, 144, 255, 0.08) 0%, rgba(114, 46, 209, 0.08) 100%)',
          borderRadius: 16,
          border: '1px solid rgba(255,255,255,0.2)'
        }}
      >
        <Row align="middle" justify="space-between" gutter={[16, 16]}>
          <Col>
            <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
              <div
                style={{
                  width: 64,
                  height: 64,
                  borderRadius: '50%',
                  background: 'var(--primary-color, #1890ff)',
                  color: '#fff',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 28,
                  fontWeight: 700
                }}
              >
                {profile.fullName.substring(0, 2).toUpperCase()}
              </div>
              <div>
                <h2 style={{ margin: 0, fontSize: 24, fontWeight: 700 }}>
                  {profile.fullName}{' '}
                  <Tag color={isGermany ? 'gold' : 'red'} style={{ marginLeft: 8, fontSize: 13 }}>
                    {isGermany ? '🇩🇪 Germany (DE)' : '🇹🇷 Türkiye (TR)'}
                  </Tag>
                </h2>
                <div style={{ color: 'var(--text-secondary, #666)', marginTop: 4 }}>
                  <span>{profile.jobTitle}</span> • <span>{profile.departmentName}</span> • <span>{profile.email}</span>
                </div>
              </div>
            </div>
          </Col>
          <Col>
            <Tag color="blue" icon={<SafetyCertificateOutlined />}>
              {profile.employmentType}
            </Tag>
            {profile.trialPeriodEndDate && (
              <Tag color="purple" icon={<CalendarOutlined />}>
                {t('hr.trialPeriod', 'Trial Period Until')}: {dayjs(profile.trialPeriodEndDate).format('DD MMM YYYY')}
              </Tag>
            )}
          </Col>
        </Row>
      </Card>

      {/* Main Tabs */}
      <Card style={{ borderRadius: 16 }}>
        <Tabs
          defaultActiveKey="overview"
          items={[
            {
              key: 'overview',
              label: (
                <span>
                  <UserOutlined />
                  {t('hr.overviewTab', 'Overview & Personal')}
                </span>
              ),
              children: (
                <Form
                  form={form}
                  layout="vertical"
                  disabled={!isEditing}
                  onFinish={handleProfileSave}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
                    <h3>Personal Information & Contact</h3>
                    {!isEditing ? (
                      <Button type="primary" onClick={() => setIsEditing(true)}>
                        Edit Profile
                      </Button>
                    ) : (
                      <div style={{ display: 'flex', gap: 8 }}>
                        <Button onClick={() => setIsEditing(false)}>Cancel</Button>
                        <Button type="primary" htmlType="submit">
                          Save Changes
                        </Button>
                      </div>
                    )}
                  </div>

                  <Row gutter={16}>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item name="country" label="Country & Jurisdiction">
                        <Select options={[{ value: 'TR', label: '🇹🇷 Türkiye (TR)' }, { value: 'DE', label: '🇩🇪 Germany (DE)' }]} />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item name="birthDate" label="Birth Date">
                        <DatePicker style={{ width: '100%' }} />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item name="gender" label="Gender">
                        <Select options={[{ value: 'Female', label: 'Female' }, { value: 'Male', label: 'Male' }, { value: 'Other', label: 'Other' }]} />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item name="bloodType" label="Blood Type">
                        <Input placeholder="e.g. A Rh+" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item name="emergencyContactName" label="Emergency Contact Name">
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item name="emergencyContactPhone" label="Emergency Contact Phone">
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={16} md={16}>
                      <Form.Item name="addressLine" label="Address">
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8} md={4}>
                      <Form.Item name="city" label="City">
                        <Input />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8} md={4}>
                      <Form.Item name="postalCode" label="Postal Code">
                        <Input />
                      </Form.Item>
                    </Col>
                  </Row>
                </Form>
              )
            },
            {
              key: 'compensation',
              label: (
                <span>
                  <BankOutlined />
                  {t('hr.compensationTab', 'Compensation & Legal')}
                </span>
              ),
              children: (
                <div>
                  <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
                    <Col xs={24} sm={12} md={6}>
                      <Statistic
                        title="Gross Salary"
                        value={profile.grossSalary}
                        precision={2}
                        suffix={profile.currency === 'EUR' ? '€' : '₺'}
                      />
                    </Col>
                    <Col xs={24} sm={12} md={6}>
                      <Statistic title="Working Hours" value={profile.workingHoursPerWeek} suffix="h/week" />
                    </Col>
                    <Col xs={24} sm={12} md={6}>
                      <Statistic title="Hire Date" value={dayjs(profile.hireDate).format('DD/MM/YYYY')} />
                    </Col>
                    <Col xs={24} sm={12} md={6}>
                      <Statistic title="Reporting Manager" value={profile.managerName || 'Direct Executive'} />
                    </Col>
                  </Row>

                  <Divider />

                  <h3 style={{ marginBottom: 16 }}>
                    {isGermany ? '🇩🇪 German Tax & Social Security Details' : '🇹🇷 Türkiye TC & SGK Özlük Detayları'}
                  </h3>

                  {isGermany ? (
                    <Row gutter={16}>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="Steuer-ID (Tax ID)">
                          <strong>{profile.germanyDetails?.taxId || 'DE9876543210'}</strong>
                        </Card>
                      </Col>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="Sozialversicherungsnr. (SV)">
                          <strong>{profile.germanyDetails?.socialSecurityNo || '12 150789 S 042'}</strong>
                        </Card>
                      </Col>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="Krankenkasse (Health Ins.)">
                          <strong>{profile.germanyDetails?.healthInsuranceProvider || 'Techniker Krankenkasse (TK)'}</strong>
                        </Card>
                      </Col>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="Steuerklasse (Tax Class)">
                          <strong>Klasse {profile.germanyDetails?.taxClass || 1}</strong>
                        </Card>
                      </Col>
                    </Row>
                  ) : (
                    <Row gutter={16}>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="TC Kimlik No">
                          <strong>{profile.turkeyDetails?.tcKimlikNo || '12345678901'}</strong>
                        </Card>
                      </Col>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="SGK Sicil No">
                          <strong>{profile.turkeyDetails?.sgkNo || 'SGK-9876543210'}</strong>
                        </Card>
                      </Col>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="Askerlik Durumu">
                          <Tag color="green">Muaf / Yapıldı</Tag>
                        </Card>
                      </Col>
                      <Col xs={24} sm={12} md={6}>
                        <Card size="small" title="Engellilik Oranı">
                          <strong>{profile.turkeyDetails?.disabilityDegree || 0}% (Derece Yok)</strong>
                        </Card>
                      </Col>
                    </Row>
                  )}

                  <Divider />

                  <h3>Bank Account & Payout Details</h3>
                  <Row gutter={16} style={{ marginTop: 12 }}>
                    <Col xs={24} sm={12} md={8}>
                      <Form.Item label="Bank Name">
                        <Input value={profile.bankName} disabled />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={12}>
                      <Form.Item label="IBAN">
                        <Input value={profile.iban} disabled />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={12} md={4}>
                      <Form.Item label="BIC / SWIFT">
                        <Input value={profile.bicSwift || 'BANKTRIS'} disabled />
                      </Form.Item>
                    </Col>
                  </Row>
                </div>
              )
            },
            {
              key: 'documents',
              label: (
                <span>
                  <FileTextOutlined />
                  {t('hr.documentsTab', 'Digital Document Vault')}
                </span>
              ),
              children: (
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
                    <h3>Özlük Evrakları & Legal Certificates</h3>
                    <Button type="primary" icon={<UploadOutlined />} onClick={() => setIsUploadModalOpen(true)}>
                      Upload Document
                    </Button>
                  </div>

                  <Table
                    dataSource={documents}
                    rowKey="id"
                    columns={[
                      {
                        title: 'Category',
                        dataIndex: 'category',
                        render: (cat: string) => <Tag color="blue">{cat}</Tag>
                      },
                      { title: 'Title', dataIndex: 'title' },
                      { title: 'File Name', dataIndex: 'fileName' },
                      {
                        title: 'Size',
                        dataIndex: 'fileSizeBytes',
                        render: (bytes: number) => `${(bytes / 1024).toFixed(1)} KB`
                      },
                      {
                        title: 'Uploaded Date',
                        dataIndex: 'uploadedAt',
                        render: (d: string) => dayjs(d).format('DD MMM YYYY')
                      },
                      {
                        title: 'Expiration Status',
                        dataIndex: 'expirationDate',
                        render: (exp: string, record: EmployeeDocument) => {
                          if (!exp) return <Tag color="default">No Expiry</Tag>;
                          if (record.isExpired) return <Tag color="red" icon={<ExclamationCircleOutlined />}>Expired ({dayjs(exp).format('DD/MM/YYYY')})</Tag>;
                          if (record.isExpiringSoon) return <Tag color="warning" icon={<ExclamationCircleOutlined />}>Expires Soon ({dayjs(exp).format('DD/MM/YYYY')})</Tag>;
                          return <Tag color="green" icon={<CheckCircleOutlined />}>Valid until {dayjs(exp).format('DD/MM/YYYY')}</Tag>;
                        }
                      }
                    ]}
                  />
                </div>
              )
            },
            {
              key: 'assets',
              label: (
                <span>
                  <LaptopOutlined />
                  {t('hr.assetsTab', 'Assigned Assets')}
                </span>
              ),
              children: (
                <div>
                  <h3 style={{ marginBottom: 16 }}>Zimmetli Varlıklar & Equipment Handover</h3>
                  <Row gutter={[16, 16]}>
                    {profile.assignedAssets.map((asset) => (
                      <Col xs={24} sm={12} md={8} key={asset.id}>
                        <Card size="small" style={{ borderRadius: 12, border: '1px solid #e8e8e8' }}>
                          <Tag color="cyan" style={{ marginBottom: 8 }}>{asset.category}</Tag>
                          <h4 style={{ margin: 0 }}>{asset.model}</h4>
                          <div style={{ color: '#8c8c8c', fontSize: 12, marginTop: 4 }}>Tag: {asset.assetTag}</div>
                          <Tag color="green" style={{ marginTop: 8 }}>{asset.status}</Tag>
                        </Card>
                      </Col>
                    ))}
                    {profile.assignedAssets.length === 0 && (
                      <Col span={24}>
                        <Alert message="No active equipment assigned to your account." type="info" showIcon />
                      </Col>
                    )}
                  </Row>
                </div>
              )
            },
            {
              key: 'leaves',
              label: (
                <span>
                  <CalendarOutlined />
                  {t('hr.leavesTab', 'Leave Entitlements')}
                </span>
              ),
              children: (
                <div>
                  <h3 style={{ marginBottom: 16 }}>
                    {isGermany ? '🇩🇪 Erholungsurlaub & Abwesenheiten' : '🇹🇷 Yıllık İzin & Hakediş Balansları'}
                  </h3>
                  <Row gutter={[16, 16]}>
                    {profile.leaveBalances.map((bal, i) => {
                      const percent = Math.round((bal.usedDays / bal.totalEntitlementDays) * 100);
                      return (
                        <Col xs={24} sm={12} md={8} key={i}>
                          <Card style={{ borderRadius: 12 }}>
                            <h4>{bal.leaveType}</h4>
                            <Progress percent={percent} status={percent > 80 ? 'exception' : 'active'} />
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 12 }}>
                              <span>Entitled: <strong>{bal.totalEntitlementDays} days</strong></span>
                              <span>Remaining: <strong style={{ color: '#52c41a' }}>{bal.remainingDays} days</strong></span>
                            </div>
                          </Card>
                        </Col>
                      );
                    })}
                  </Row>
                </div>
              )
            }
          ]}
        />
      </Card>

      {/* Upload Document Modal */}
      <Modal
        title="Upload Özlük Document"
        open={isUploadModalOpen}
        onCancel={() => setIsUploadModalOpen(false)}
        footer={null}
      >
        <Form form={uploadForm} layout="vertical" onFinish={handleDocumentUpload}>
          <Form.Item name="category" label="Category" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 'Contract', label: 'Employment Contract' },
                { value: 'IDCard', label: 'ID Card / Passport' },
                { value: 'TaxCertificate', label: 'Tax / Social Security Certificate' },
                { value: 'SickLeaveAU', label: 'AU Krankmeldung / Health Report' },
                { value: 'Diploma', label: 'Diploma / Certificate' },
                { value: 'NDA', label: 'NDA / Policy Agreement' }
              ]}
            />
          </Form.Item>

          <Form.Item name="title" label="Document Title" rules={[{ required: true }]}>
            <Input placeholder="e.g. 2026 Employment Agreement" />
          </Form.Item>

          <Form.Item name="expirationDate" label="Expiration Date (Optional)">
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="file" label="Select File" rules={[{ required: true }]}>
            <Upload beforeUpload={() => false} maxCount={1}>
              <Button icon={<UploadOutlined />}>Choose File (PDF, Image)</Button>
            </Upload>
          </Form.Item>

          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsUploadModalOpen(false)} style={{ marginRight: 8 }}>
              Cancel
            </Button>
            <Button type="primary" htmlType="submit" loading={uploading}>
              Upload Document
            </Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

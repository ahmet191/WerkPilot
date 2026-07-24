import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Rate, Alert, Row, Col, Spin, Divider } from 'antd';
import { TrophyOutlined, ExclamationCircleOutlined, CheckCircleOutlined } from '@ant-design/icons';
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

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/hr/skills-certificates');
      setSkills(res.data.skills);
      setCertificates(res.data.certificates);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

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
            <h3 style={{ marginBottom: 16 }}>🎯 Skill Matrix & Competency Evaluations</h3>
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
            </Row>

            <Divider />

            <h3 style={{ marginBottom: 16 }}>📜 Certification Vault & Expiry Alarms</h3>
            <Table
              dataSource={certificates}
              rowKey="id"
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
    </div>
  );
};

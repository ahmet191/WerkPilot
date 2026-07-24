import React, { useEffect, useState } from 'react';
import { Card, Col, Row, Typography, Spin, Button, Tag } from 'antd';
import { Link } from 'react-router-dom';
import {
  CustomerServiceOutlined,
  UserOutlined,
  BankOutlined,
  FileTextOutlined,
  BuildOutlined,
  LockOutlined,
  ArrowRightOutlined
} from '@ant-design/icons';
import { api } from '../../../core/api';

const { Title, Text } = Typography;

const resolveDeskIcon = (iconName: string) => {
  switch (iconName) {
    case 'UserOutlined': return <UserOutlined />;
    case 'BankOutlined': return <BankOutlined />;
    case 'FileTextOutlined': return <FileTextOutlined />;
    case 'BuildOutlined': return <BuildOutlined />;
    default: return <CustomerServiceOutlined />;
  }
};

export const ServiceDeskPortal: React.FC = () => {
  const [desks, setDesks] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/visibility/my-desks')
      .then(res => {
        setDesks(res.data);
        setLoading(false);
      })
      .catch(() => {
        // Fallback fetch
        api.get('/servicedesks?pageSize=100')
          .then(res => {
            setDesks(res.data.items || res.data || []);
            setLoading(false);
          })
          .catch(() => setLoading(false));
      });
  }, []);

  if (loading) {
    return <div style={{ textAlign: 'center', padding: '50px' }}><Spin size="large" /></div>;
  }

  return (
    <div>
      <div style={{ marginBottom: 22 }}>
        <Title level={3} style={{ margin: 0, fontWeight: 700 }}>Multi-Department ESM Service Portal</Title>
        <Text type="secondary">Select a specialized department desk to submit requests or access services.</Text>
      </div>

      <Row gutter={[20, 20]}>
        {desks.map((desk: any, index: number) => (
          <Col xs={24} sm={12} md={8} lg={6} key={desk.id}>
            <Card
              className="card-lift"
              style={{
                height: '100%',
                borderRadius: 'var(--radius-md)',
                animation: `fadeInUp 0.3s ${0.05 * index}s cubic-bezier(0.4,0,0.2,1) both`,
              }}
              actions={[
                <Link to={`/servicedesk/${desk.id}/tickets`} key="enter">
                  <Button type="primary" icon={<ArrowRightOutlined />}>Enter Desk</Button>
                </Link>
              ]}
            >
              <div style={{ marginBottom: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span
                  className="stat-icon-chip"
                  style={{
                    width: 44,
                    height: 44,
                    fontSize: 20,
                    borderRadius: 12,
                    background: desk.colorHex || '#1890ff',
                    color: '#fff',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center'
                  }}
                >
                  {resolveDeskIcon(desk.iconName)}
                </span>
                {desk.isConfidential && (
                  <Tag color="purple" icon={<LockOutlined />}>Strict Privacy</Tag>
                )}
              </div>

              <Card.Meta
                title={
                  <span style={{ fontWeight: 650, fontSize: 16 }}>
                    {desk.name}
                  </span>
                }
                description={
                  <div style={{ minHeight: 48, fontSize: 13, color: 'var(--text-secondary)' }}>
                    {desk.description || 'Enterprise Service Management Desk'}
                  </div>
                }
              />
            </Card>
          </Col>
        ))}
      </Row>
    </div>
  );
};

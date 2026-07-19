import React, { useEffect, useState } from 'react';
import { Card, Col, Row, Typography, Spin, Button } from 'antd';
import { Link } from 'react-router-dom';
import { AppstoreOutlined, ArrowRightOutlined } from '@ant-design/icons';
import { api, unwrapList } from '../../../core/api';

const { Title, Text } = Typography;

export const ServiceDeskPortal: React.FC = () => {
    const [desks, setDesks] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Fetch Service Desks from our API
        api.get('/servicedesks?pageSize=100')
            .then(res => {
                setDesks(unwrapList(res.data));
                setLoading(false);
            })
            .catch(() => setLoading(false));
    }, []);

    if (loading) {
        return <div style={{ textAlign: 'center', padding: '50px' }}><Spin size="large" /></div>;
    }

    return (
        <div>
            <div style={{ marginBottom: 22 }}>
                <Title level={3} style={{ margin: 0, fontWeight: 700 }}>Service Desk Portal</Title>
                <Text type="secondary">Select a service desk to request help or services.</Text>
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
                            <Card.Meta
                                avatar={
                                    <span
                                        className="stat-icon-chip"
                                        style={{ width: 40, height: 40, fontSize: 18, background: 'var(--accent-gradient)' }}
                                    >
                                        <AppstoreOutlined />
                                    </span>
                                }
                                title={<span style={{ fontWeight: 650 }}>{desk.name}</span>}
                                description={desk.description || 'No description available.'}
                            />
                        </Card>
                    </Col>
                ))}
            </Row>
        </div>
    );
};

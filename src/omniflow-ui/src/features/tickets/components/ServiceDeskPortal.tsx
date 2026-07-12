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
        <div style={{ padding: '24px' }}>
            <Title level={2}>Service Desk Portal</Title>
            <Text type="secondary" style={{ display: 'block', marginBottom: '24px' }}>
                Select a service desk to request help or services.
            </Text>
            
            <Row gutter={[24, 24]}>
                {desks.map((desk: any) => (
                    <Col xs={24} sm={12} md={8} lg={6} key={desk.id}>
                        <Card 
                            hoverable
                            style={{ height: '100%', borderRadius: '12px' }}
                            actions={[
                                <Link to={`/servicedesk/${desk.id}/tickets`} key="enter">
                                    <Button type="primary" icon={<ArrowRightOutlined />}>Enter Desk</Button>
                                </Link>
                            ]}
                        >
                            <Card.Meta 
                                avatar={<AppstoreOutlined style={{ fontSize: '24px', color: '#1890ff' }} />}
                                title={desk.name}
                                description={desk.description || 'No description available.'}
                            />
                        </Card>
                    </Col>
                ))}
            </Row>
        </div>
    );
};

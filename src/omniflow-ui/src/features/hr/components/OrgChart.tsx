import React, { useEffect, useState } from 'react';
import { Card, Typography, Spin, Avatar } from 'antd';
import { UserOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';
// Normally we would use an Org Chart library like react-organization-chart, 
// but for simplicity we'll render a structured tree or basic lists.

const { Title, Text } = Typography;

export const OrgChart: React.FC = () => {
    const [users, setUsers] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        api.get('/directory/orgchart')
            .then(res => {
                setUsers(res.data);
                setLoading(false);
            })
            .catch(() => setLoading(false));
    }, []);

    const buildHierarchy = () => {
        const map = new Map<string, any>();
        const roots: any[] = [];

        users.forEach(u => map.set(u.id, { ...u, children: [] }));
        users.forEach(u => {
            if (u.managerId && map.has(u.managerId)) {
                map.get(u.managerId).children.push(map.get(u.id));
            } else {
                roots.push(map.get(u.id));
            }
        });

        return roots;
    };

    const renderNode = (node: any) => (
        <div key={node.id} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', margin: '0 20px' }}>
            <Card size="small" style={{ width: 200, textAlign: 'center', marginBottom: '16px', borderColor: 'var(--primary-color)' }}>
                <Avatar icon={<UserOutlined />} style={{ marginBottom: '8px', backgroundColor: 'var(--primary-color)' }} />
                <Title level={5} style={{ margin: 0 }}>{node.name}</Title>
                <Text type="secondary" style={{ fontSize: '12px' }}>{node.title || 'Employee'}</Text>
            </Card>
            {node.children && node.children.length > 0 && (
                <div style={{ display: 'flex', justifyContent: 'center', position: 'relative', paddingTop: '20px' }}>
                    {/* Very basic hierarchical line simulation */}
                    <div style={{ position: 'absolute', top: 0, width: '1px', height: '20px', backgroundColor: '#ccc' }}></div>
                    {node.children.map((child: any) => renderNode(child))}
                </div>
            )}
        </div>
    );

    if (loading) return <div style={{ textAlign: 'center', padding: '50px' }}><Spin size="large" /></div>;

    const hierarchy = buildHierarchy();

    return (
        <div style={{ padding: '24px', overflowX: 'auto' }}>
            <Title level={2} style={{ marginBottom: '40px', textAlign: 'center' }}>Organization Chart</Title>
            <div style={{ display: 'flex', justifyContent: 'center', minWidth: 'min-content' }}>
                {hierarchy.map(root => renderNode(root))}
            </div>
        </div>
    );
};

import React, { useEffect, useState } from 'react';
import { Card, Col, Row, Typography, Spin, Avatar, Input, Tag } from 'antd';
import { UserOutlined, MailOutlined, PhoneOutlined, SolutionOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;
const { Search } = Input;

export const EmployeeDirectory: React.FC = () => {
    const [employees, setEmployees] = useState<any[]>([]);
    const [filteredEmployees, setFilteredEmployees] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch('http://localhost:5226/api/directory/employees')
            .then(res => res.json())
            .then(data => {
                setEmployees(data);
                setFilteredEmployees(data);
                setLoading(false);
            });
    }, []);

    const onSearch = (value: string) => {
        const lowercased = value.toLowerCase();
        setFilteredEmployees(
            employees.filter(emp => 
                emp.name?.toLowerCase().includes(lowercased) ||
                emp.department?.toLowerCase().includes(lowercased) ||
                emp.title?.toLowerCase().includes(lowercased)
            )
        );
    };

    if (loading) return <div style={{ textAlign: 'center', padding: '50px' }}><Spin size="large" /></div>;

    return (
        <div style={{ padding: '24px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
                <Title level={2} style={{ margin: 0 }}>Employee Directory</Title>
                <Search placeholder="Search by name, role, department..." onSearch={onSearch} style={{ width: 300 }} />
            </div>

            <Row gutter={[24, 24]}>
                {filteredEmployees.map((emp: any) => (
                    <Col xs={24} sm={12} md={8} lg={6} key={emp.userId}>
                        <Card hoverable style={{ height: '100%', borderRadius: '12px', textAlign: 'center' }}>
                            <Avatar size={64} icon={<UserOutlined />} src={emp.profilePictureUrl} style={{ marginBottom: '16px' }} />
                            <Title level={5} style={{ margin: 0 }}>{emp.name}</Title>
                            <Text type="secondary" style={{ display: 'block', marginBottom: '8px' }}>{emp.title}</Text>
                            
                            <Tag color="blue" style={{ marginBottom: '16px' }}>{emp.department}</Tag>
                            
                            {emp.email && <div style={{ marginBottom: '4px' }}><MailOutlined style={{ marginRight: '8px' }} /><Text>{emp.email}</Text></div>}
                            {emp.phone && <div style={{ marginBottom: '4px' }}><PhoneOutlined style={{ marginRight: '8px' }} /><Text>{emp.phone}</Text></div>}
                            
                            {emp.skills && emp.skills.length > 0 && (
                                <div style={{ marginTop: '16px', textAlign: 'left' }}>
                                    <SolutionOutlined style={{ marginRight: '8px' }} />
                                    {emp.skills.map((skill: string) => <Tag key={skill} style={{ marginTop: '4px' }}>{skill}</Tag>)}
                                </div>
                            )}
                        </Card>
                    </Col>
                ))}
            </Row>
        </div>
    );
};

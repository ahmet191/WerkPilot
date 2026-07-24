import React, { useEffect, useState } from 'react';
import { Card, Col, Row, Typography, Spin, Avatar, Input, Tag, Modal, Button, Descriptions, Space } from 'antd';
import { UserOutlined, MailOutlined, PhoneOutlined, SolutionOutlined, IdcardOutlined, EnvironmentOutlined, TeamOutlined, SettingOutlined } from '@ant-design/icons';
import { api } from '../../../core/api';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;
const { Search } = Input;

export const EmployeeDirectory: React.FC = () => {
    const [employees, setEmployees] = useState<any[]>([]);
    const [filteredEmployees, setFilteredEmployees] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [selectedEmployee, setSelectedEmployee] = useState<any | null>(null);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        api.get('/directory/employees')
            .then(res => {
                setEmployees(res.data);
                setFilteredEmployees(res.data);
                setLoading(false);
            })
            .catch(() => setLoading(false));
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

    const openProfile = (emp: any) => {
        setSelectedEmployee(emp);
        setIsModalOpen(true);
    };

    if (loading) return <div style={{ textAlign: 'center', padding: '50px' }}><Spin size="large" /></div>;

    return (
        <div style={{ padding: '24px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
                <div>
                    <Title level={2} style={{ margin: 0 }}><TeamOutlined style={{ marginRight: 8, color: '#1890ff' }} /> Employee Directory</Title>
                    <Text type="secondary">Browse and connect with colleagues across the organization.</Text>
                </div>
                <Search placeholder="Search by name, role, department..." onSearch={onSearch} style={{ width: 300 }} size="large" allowClear />
            </div>

            <Row gutter={[24, 24]}>
                {filteredEmployees.map((emp: any) => (
                    <Col xs={24} sm={12} md={8} lg={6} key={emp.userId}>
                        <Card 
                            hoverable 
                            style={{ height: '100%', borderRadius: '12px', textAlign: 'center' }}
                            onClick={() => openProfile(emp)}
                        >
                            <Avatar size={64} icon={<UserOutlined />} src={emp.profilePictureUrl} style={{ marginBottom: '16px' }} />
                            <Title level={5} style={{ margin: 0 }}>{emp.name}</Title>
                            <Text type="secondary" style={{ display: 'block', marginBottom: '8px' }}>{emp.title}</Text>
                            
                            <Tag color="blue" style={{ marginBottom: '16px' }}>{emp.department}</Tag>
                            
                            {emp.email && <div style={{ marginBottom: '4px', fontSize: 13, color: '#595959' }}><MailOutlined style={{ marginRight: '8px' }} />{emp.email}</div>}
                            {emp.phone && <div style={{ marginBottom: '4px', fontSize: 13, color: '#595959' }}><PhoneOutlined style={{ marginRight: '8px' }} />{emp.phone}</div>}
                        </Card>
                    </Col>
                ))}
            </Row>

            <Modal
                title="Employee Profile"
                open={isModalOpen}
                onCancel={() => setIsModalOpen(false)}
                footer={
                    <Button type="primary" icon={<SettingOutlined />} onClick={() => navigate('/users')}>
                        Manage in User Admin
                    </Button>
                }
                width={600}
                destroyOnClose
            >
                {selectedEmployee && (
                    <div style={{ paddingTop: 16 }}>
                        <div style={{ display: 'flex', alignItems: 'center', marginBottom: 24 }}>
                            <Avatar size={80} icon={<UserOutlined />} src={selectedEmployee.profilePictureUrl} style={{ marginRight: 24 }} />
                            <div>
                                <Title level={3} style={{ margin: 0 }}>{selectedEmployee.name}</Title>
                                <Text style={{ fontSize: 16, color: '#595959' }}>{selectedEmployee.title}</Text>
                                <div style={{ marginTop: 8 }}>
                                    <Tag color="blue">{selectedEmployee.department}</Tag>
                                </div>
                            </div>
                        </div>

                        <Descriptions bordered column={1} size="small" style={{ marginBottom: 24 }}>
                            <Descriptions.Item label={<Space><MailOutlined /> Email</Space>}>
                                <a href={`mailto:${selectedEmployee.email}`}>{selectedEmployee.email}</a>
                            </Descriptions.Item>
                            <Descriptions.Item label={<Space><PhoneOutlined /> Phone</Space>}>
                                {selectedEmployee.phone || 'N/A'}
                            </Descriptions.Item>
                            <Descriptions.Item label={<Space><IdcardOutlined /> Employee ID</Space>}>
                                {selectedEmployee.userId.substring(0, 8).toUpperCase()}
                            </Descriptions.Item>
                            <Descriptions.Item label={<Space><EnvironmentOutlined /> Location</Space>}>
                                {selectedEmployee.location || 'Headquarters'}
                            </Descriptions.Item>
                        </Descriptions>

                        {selectedEmployee.skills && selectedEmployee.skills.length > 0 && (
                            <div>
                                <Text strong style={{ display: 'flex', alignItems: 'center', marginBottom: 12 }}>
                                    <SolutionOutlined style={{ marginRight: 8 }} /> Skills & Expertise
                                </Text>
                                <div>
                                    {selectedEmployee.skills.map((skill: string) => (
                                        <Tag key={skill} color="cyan" style={{ marginBottom: 8, padding: '4px 12px' }}>{skill}</Tag>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                )}
            </Modal>
        </div>
    );
};

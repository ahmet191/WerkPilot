import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, Switch, Row, Col, message, Spin, ColorPicker } from 'antd';
import { BankOutlined, PlusOutlined, ApartmentOutlined } from '@ant-design/icons';
import { api } from '../../core/api';

interface CustomDepartment {
  id: string;
  code: string;
  name: string;
  description: string;
  departmentType: string;
  iconName: string;
  colorHex: string;
  parentDepartmentId?: string;
  parentDepartmentName?: string;
  isActive: boolean;
}

export const DepartmentStudioPage: React.FC = () => {
  const [departments, setDepartments] = useState<CustomDepartment[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();

  const fetchDepartments = async () => {
    setLoading(true);
    try {
      const res = await api.get('/departments/studio');
      setDepartments(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDepartments();
  }, []);

  const handleSubmit = async (values: any) => {
    try {
      const payload = {
        ...values,
        colorHex: typeof values.colorHex === 'string' ? values.colorHex : values.colorHex?.toHexString() || '#8b5cf6'
      };
      await api.post('/departments/studio', payload);
      message.success('Custom department saved successfully!');
      setIsModalOpen(false);
      form.resetFields();
      fetchDepartments();
    } catch (err) {
      message.error('Failed to save department.');
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <BankOutlined style={{ marginRight: 8, color: '#8b5cf6' }} />
              Custom Department Studio
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Create and manage dynamic enterprise departments (IT, HR, Finance, Legal, Facilities) with custom themes and parent-child hierarchy.
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
            Add Custom Department
          </Button>
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={departments}
            rowKey="id"
            columns={[
              {
                title: 'Dept Code',
                dataIndex: 'code',
                render: (code: string, record: CustomDepartment) => (
                  <Tag style={{ background: record.colorHex, color: '#fff', fontWeight: 700 }}>
                    {code}
                  </Tag>
                )
              },
              { title: 'Department Name', dataIndex: 'name', render: (n: string) => <strong>{n}</strong> },
              { title: 'Description', dataIndex: 'description' },
              {
                title: 'Type',
                dataIndex: 'departmentType',
                render: (type: string) => <Tag color="purple">{type}</Tag>
              },
              {
                title: 'Parent Department',
                dataIndex: 'parentDepartmentName',
                render: (p: string) => p ? <Tag icon={<ApartmentOutlined />}>{p}</Tag> : <span style={{ color: '#8c8c8c' }}>Root Department</span>
              },
              {
                title: 'Status',
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">Active</Tag> : <Tag color="default">Inactive</Tag>
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title="Create / Edit Custom Department"
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ departmentType: 'Custom', colorHex: '#8b5cf6', isActive: true }}>
          <Form.Item name="name" label="Department Name" rules={[{ required: true }]}>
            <Input placeholder="e.g. Legal & Contract Management" />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="code" label="Dept Code">
                <Input placeholder="e.g. LEGAL" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="departmentType" label="Department Type">
                <Select options={[
                  { value: 'IT', label: 'IT & Infrastructure' },
                  { value: 'HR', label: 'Human Resources' },
                  { value: 'Finance', label: 'Finance & Procurement' },
                  { value: 'Legal', label: 'Legal & Compliance' },
                  { value: 'Facilities', label: 'Facility Management' },
                  { value: 'Custom', label: 'Custom Department' }
                ]} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="description" label="Description">
            <Input.TextArea placeholder="Responsibilities and scope..." />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="colorHex" label="Theme Color">
                <ColorPicker format="hex" showText />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="isActive" label="Is Active" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit">Save Department</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, Switch, Row, Col, message, Spin, ColorPicker, Popconfirm, Space } from 'antd';
import { BankOutlined, PlusOutlined, ApartmentOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
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

const DEFAULTS = { departmentType: 'Custom', colorHex: '#8b5cf6', isActive: true };

export const DepartmentStudioPage: React.FC = () => {
  const [departments, setDepartments] = useState<CustomDepartment[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm();

  const fetchDepartments = async () => {
    setLoading(true);
    try {
      const res = await api.get('/departments/studio');
      setDepartments(res.data);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDepartments();
  }, []);

  const openModal = (dept?: CustomDepartment) => {
    if (dept) {
      setEditingId(dept.id);
      form.setFieldsValue({
        name: dept.name, code: dept.code, departmentType: dept.departmentType,
        description: dept.description, colorHex: dept.colorHex, isActive: dept.isActive,
      });
    } else {
      setEditingId(null);
      form.resetFields();
      form.setFieldsValue(DEFAULTS);
    }
    setIsModalOpen(true);
  };

  const closeModal = () => { setIsModalOpen(false); setEditingId(null); };

  const handleSubmit = async (values: any) => {
    setSubmitting(true);
    try {
      const payload = {
        ...values,
        id: editingId ?? undefined,
        colorHex: typeof values.colorHex === 'string' ? values.colorHex : values.colorHex?.toHexString() || '#8b5cf6'
      };
      await api.post('/departments/studio', payload);
      message.success(editingId ? 'Department updated.' : 'Custom department created.');
      closeModal();
      form.resetFields();
      fetchDepartments();
    } catch {
      message.error('Failed to save department.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/departments/studio/${id}`);
      message.success('Department removed.');
      fetchDepartments();
    } catch {
      message.error('Failed to remove department.');
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
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
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
              },
              {
                title: 'Actions',
                key: 'actions',
                width: 100,
                render: (_: unknown, record: CustomDepartment) => (
                  <Space size={0}>
                    <Button type="text" size="small" icon={<EditOutlined />} onClick={() => openModal(record)} />
                    <Popconfirm title="Remove this department?" onConfirm={() => handleDelete(record.id)} okText="Remove" cancelText="Cancel">
                      <Button type="text" size="small" danger icon={<DeleteOutlined />} />
                    </Popconfirm>
                  </Space>
                )
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title={editingId ? 'Edit Custom Department' : 'Create Custom Department'}
        open={isModalOpen}
        onCancel={closeModal}
        footer={null}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={DEFAULTS}>
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
            <Button onClick={closeModal} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>{editingId ? 'Save' : 'Create Department'}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

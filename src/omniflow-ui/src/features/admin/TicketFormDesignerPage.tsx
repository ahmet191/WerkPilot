import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, Switch, message, Spin } from 'antd';
import { FileTextOutlined, PlusOutlined } from '@ant-design/icons';
import { api } from '../../core/api';

interface FormSchema {
  id: string;
  serviceDeskId: string;
  serviceDeskName: string;
  formTitle: string;
  formDescription: string;
  fieldsJson: string;
  isActive: boolean;
}

export const TicketFormDesignerPage: React.FC = () => {
  const [schemas, setSchemas] = useState<FormSchema[]>([]);
  const [desks, setDesks] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();

  const fetchData = async () => {
    setLoading(true);
    try {
      const [resSchemas, resDesks] = await Promise.all([
        api.get('/forms/schemas'),
        api.get('/visibility/my-desks')
      ]);
      setSchemas(resSchemas.data);
      setDesks(resDesks.data || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSubmit = async (values: any) => {
    try {
      await api.post('/forms/schemas', values);
      message.success('Form schema saved successfully!');
      setIsModalOpen(false);
      form.resetFields();
      fetchData();
    } catch (err) {
      message.error('Failed to save form schema.');
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <FileTextOutlined style={{ marginRight: 8, color: '#1890ff' }} />
              Dynamic Ticket Form Designer
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Design custom request form schemas and dynamic input fields per Service Desk.
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
            Create Form Schema
          </Button>
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={schemas}
            rowKey="id"
            columns={[
              { title: 'Service Desk', dataIndex: 'serviceDeskName', render: (s: string) => <Tag color="blue">{s}</Tag> },
              { title: 'Form Title', dataIndex: 'formTitle', render: (t: string) => <strong>{t}</strong> },
              { title: 'Description', dataIndex: 'formDescription' },
              {
                title: 'Fields Count',
                dataIndex: 'fieldsJson',
                render: (json: string) => {
                  try {
                    const parsed = JSON.parse(json);
                    return <Tag color="cyan">{parsed.length} Dynamic Fields</Tag>;
                  } catch {
                    return <Tag color="default">0 Fields</Tag>;
                  }
                }
              },
              {
                title: 'Status',
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">Active</Tag> : <Tag color="default">Draft</Tag>
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title="Design Service Desk Ticket Form Schema"
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
        width={600}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ isActive: true, fieldsJson: '[\n  { "key": "device_type", "label": "Device Type", "fieldType": "Select", "isRequired": true, "options": ["MacBook", "Dell XPS"] }\n]' }}>
          <Form.Item name="serviceDeskId" label="Target Service Desk" rules={[{ required: true }]}>
            <Select options={desks.map(d => ({ value: d.id, label: d.name }))} placeholder="Select Service Desk" />
          </Form.Item>
          <Form.Item name="formTitle" label="Form Title" rules={[{ required: true }]}>
            <Input placeholder="e.g. Hardware Acquisition Request Form" />
          </Form.Item>
          <Form.Item name="formDescription" label="Form Description">
            <Input.TextArea placeholder="Brief instructions for ticket requestors..." />
          </Form.Item>
          <Form.Item name="fieldsJson" label="Fields Definition (JSON)" rules={[{ required: true }]}>
            <Input.TextArea rows={6} style={{ fontFamily: 'monospace' }} />
          </Form.Item>
          <Form.Item name="isActive" label="Is Form Active" valuePropName="checked">
            <Switch />
          </Form.Item>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit">Save Form Schema</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

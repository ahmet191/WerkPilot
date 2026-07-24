import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, Switch, message, Spin, Popconfirm, Space } from 'antd';
import { FileTextOutlined, PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

interface FormSchema {
  id: string;
  serviceDeskId: string;
  serviceDeskName: string;
  formTitle: string;
  formDescription: string;
  fieldsJson: string;
  isActive: boolean;
}

const SAMPLE_FIELDS = '[\n  { "key": "device_type", "label": "Device Type", "fieldType": "Select", "isRequired": true, "options": ["MacBook", "Dell XPS"] }\n]';

export const TicketFormDesignerPage: React.FC = () => {
  const { t } = useTranslation();
  const [schemas, setSchemas] = useState<FormSchema[]>([]);
  const [desks, setDesks] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
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
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const openModal = (schema?: FormSchema) => {
    if (schema) {
      setEditingId(schema.id);
      form.setFieldsValue({
        serviceDeskId: schema.serviceDeskId, formTitle: schema.formTitle,
        formDescription: schema.formDescription, fieldsJson: schema.fieldsJson, isActive: schema.isActive,
      });
    } else {
      setEditingId(null);
      form.resetFields();
      form.setFieldsValue({ isActive: true, fieldsJson: SAMPLE_FIELDS });
    }
    setIsModalOpen(true);
  };

  const closeModal = () => { setIsModalOpen(false); setEditingId(null); };

  const handleSubmit = async (values: any) => {
    setSubmitting(true);
    try {
      await api.post('/forms/schemas', editingId ? { ...values, id: editingId } : values);
      message.success(editingId ? t('fdes.updated', 'Form schema updated.') : t('fdes.created', 'Form schema created.'));
      closeModal();
      form.resetFields();
      fetchData();
    } catch {
      message.error(t('fdes.saveFailed', 'Failed to save form schema.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/forms/schemas/${id}`);
      message.success(t('fdes.removed', 'Form schema removed.'));
      fetchData();
    } catch {
      message.error(t('fdes.removeFailed', 'Failed to remove form schema.'));
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <FileTextOutlined style={{ marginRight: 8, color: '#1890ff' }} />
              {t('fdes.title', 'Dynamic Ticket Form Designer')}
            </h2>
            <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
              {t('fdes.subtitle', 'Design custom request form schemas and dynamic input fields per Service Desk.')}
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>
            {t('fdes.createSchema', 'Create Form Schema')}
          </Button>
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={schemas}
            rowKey="id"
            columns={[
              { title: t('fdes.colDesk', 'Service Desk'), dataIndex: 'serviceDeskName', render: (s: string) => <Tag color="blue">{s}</Tag> },
              { title: t('fdes.colTitle', 'Form Title'), dataIndex: 'formTitle', render: (tt: string) => <strong>{tt}</strong> },
              { title: t('fdes.colDescription', 'Description'), dataIndex: 'formDescription' },
              {
                title: t('fdes.colFields', 'Fields Count'),
                dataIndex: 'fieldsJson',
                render: (json: string) => {
                  try {
                    const parsed = JSON.parse(json);
                    return <Tag color="cyan">{t('fdes.dynamicFields', '{n} Dynamic Fields').replace('{n}', String(parsed.length))}</Tag>;
                  } catch {
                    return <Tag color="default">{t('fdes.zeroFields', '0 Fields')}</Tag>;
                  }
                }
              },
              {
                title: t('fdes.colStatus', 'Status'),
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">{t('fdes.active', 'Active')}</Tag> : <Tag color="default">{t('fdes.draft', 'Draft')}</Tag>
              },
              {
                title: t('fdes.colActions', 'Actions'),
                key: 'actions',
                width: 100,
                render: (_: unknown, record: FormSchema) => (
                  <Space size={0}>
                    <Button type="text" size="small" icon={<EditOutlined />} onClick={() => openModal(record)} />
                    <Popconfirm title={t('fdes.confirmRemove', 'Remove this form schema?')} onConfirm={() => handleDelete(record.id)} okText={t('fdes.remove', 'Remove')} cancelText={t('common.cancel', 'Cancel')}>
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
        title={editingId ? t('fdes.editModal', 'Edit Form Schema') : t('fdes.designModal', 'Design Service Desk Ticket Form Schema')}
        open={isModalOpen}
        onCancel={closeModal}
        footer={null}
        width={600}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ isActive: true, fieldsJson: SAMPLE_FIELDS }}>
          <Form.Item name="serviceDeskId" label={t('fdes.targetDesk', 'Target Service Desk')} rules={[{ required: true }]}>
            <Select options={desks.map(d => ({ value: d.id, label: d.name }))} placeholder={t('fdes.selectDesk', 'Select Service Desk')} />
          </Form.Item>
          <Form.Item name="formTitle" label={t('fdes.colTitle', 'Form Title')} rules={[{ required: true }]}>
            <Input placeholder={t('fdes.titlePlaceholder', 'e.g. Hardware Acquisition Request Form')} />
          </Form.Item>
          <Form.Item name="formDescription" label={t('fdes.formDesc', 'Form Description')}>
            <Input.TextArea placeholder={t('fdes.descPlaceholder', 'Brief instructions for ticket requestors...')} />
          </Form.Item>
          <Form.Item name="fieldsJson" label={t('fdes.fieldsJson', 'Fields Definition (JSON)')} rules={[{ required: true }]}>
            <Input.TextArea rows={6} style={{ fontFamily: 'monospace' }} />
          </Form.Item>
          <Form.Item name="isActive" label={t('fdes.isActive', 'Is Form Active')} valuePropName="checked">
            <Switch />
          </Form.Item>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={closeModal} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>{editingId ? t('common.save', 'Save') : t('fdes.createSchemaBtn', 'Create Schema')}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

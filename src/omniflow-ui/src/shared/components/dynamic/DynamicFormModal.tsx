import React, { useState } from 'react';
import { Modal, Form, Input, Select, DatePicker, message } from 'antd';
import type { FormSchema } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';
import { SchemaService } from '../../../services/schema.service';

interface DynamicFormModalProps {
  open: boolean;
  schema: FormSchema;
  onCancel: () => void;
  onSuccess: () => void;
}

export function DynamicFormModal({ open, schema, onCancel, onSuccess }: DynamicFormModalProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);
      await SchemaService.submitFormData(schema.submitEndpoint, values);
      message.success(t('form.success', 'Saved successfully!'));
      form.resetFields();
      onSuccess();
    } catch (error) {
      console.error('Form submission failed', error);
      // Ant design validation errors are handled internally, API errors shown here
      if (error && (error as any).response) {
         message.error(t('form.error', 'Bir hata oluştu.'));
      }
    } finally {
      setLoading(false);
    }
  };

  const renderField = (field: any) => {
    switch (field.type) {
      case 'select':
        return (
          <Select placeholder={t(field.labelKey)}>
            {field.options?.map((opt: any) => (
              <Select.Option key={opt.value} value={opt.value}>
                {opt.label}
              </Select.Option>
            ))}
          </Select>
        );
      case 'date':
        return <DatePicker style={{ width: '100%' }} />;
      case 'number':
        return <Input type="number" placeholder={t(field.labelKey)} />;
      case 'text':
      default:
        return <Input placeholder={t(field.labelKey)} />;
    }
  };

  return (
    <Modal
      title={t(schema.titleKey)}
      open={open}
      onOk={handleSubmit}
      onCancel={onCancel}
      confirmLoading={loading}
      destroyOnClose
    >
      <Form form={form} layout="vertical" preserve={false}>
        {schema.fields.map((field) => (
          <Form.Item
            key={field.name}
            name={field.name}
            label={t(field.labelKey)}
            rules={[{ required: field.required, message: t('form.required', 'This field is required') }]}
          >
            {renderField(field)}
          </Form.Item>
        ))}
      </Form>
    </Modal>
  );
}

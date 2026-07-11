import React, { useState, useEffect } from 'react';
import { Modal, Form, Input, Select, DatePicker, message } from 'antd';
import type { FormSchema } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';
import { SchemaService } from '../../../services/schema.service';
import { api } from '../../../core/api';

interface DynamicFormModalProps {
  open: boolean;
  schema: FormSchema;
  onCancel: () => void;
  onSuccess: () => void;
}

const DynamicSelect = ({ field, ...props }: any) => {
  const [options, setOptions] = useState<any[]>(field.options || []);
  const [loading, setLoading] = useState(false);
  const { t } = useTranslation();

  useEffect(() => {
    if (field.dataSourceEndpoint) {
      loadOptions();
    }
  }, [field.dataSourceEndpoint]);

  const loadOptions = async () => {
    setLoading(true);
    try {
      const res = await api.get(field.dataSourceEndpoint);
      setOptions(res.data);
    } catch (err) {
      console.error(`Failed to load options for ${field.name}`, err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Select placeholder={t(field.labelKey)} loading={loading} {...props}>
      {options.map((opt: any) => (
        <Select.Option key={opt.value} value={opt.value}>
          {opt.label}
        </Select.Option>
      ))}
    </Select>
  );
};

export function DynamicFormModal({ open, schema, onCancel, onSuccess }: DynamicFormModalProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [templates, setTemplates] = useState<any[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null);
  const [activeSchema, setActiveSchema] = useState<FormSchema>(schema);

  useEffect(() => {
    if (open) {
      setSelectedTemplate(null);
      form.resetFields();
      if (schema.templateEndpoint) {
        loadTemplates();
      } else {
        setActiveSchema(schema);
      }
    }
  }, [open, schema]);

  const loadTemplates = async () => {
    setLoading(true);
    try {
      const res = await api.get(schema.templateEndpoint!);
      setTemplates(res.data);
    } catch (err) {
      console.error('Failed to load templates', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTemplateChange = async (templateId: string) => {
    setSelectedTemplate(templateId);
    if (schema.dynamicSchemaEndpoint) {
      setLoading(true);
      try {
        const url = schema.dynamicSchemaEndpoint.replace('{templateId}', templateId);
        const res = await api.get(url);
        setActiveSchema(res.data);
      } catch (err) {
        console.error('Failed to load dynamic schema', err);
      } finally {
        setLoading(false);
      }
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      // If dynamic ticket form, nest values in DynamicData except templateId/requesterId
      let payload = values;
      if (schema.templateEndpoint) {
        payload = {
          templateId: selectedTemplate,
          requesterId: "11111111-1111-1111-1111-111111111111", // Mock requester
          dynamicData: values
        };
      }

      setLoading(true);
      await SchemaService.submitFormData(activeSchema.submitEndpoint || schema.submitEndpoint, payload);
      message.success(t('form.success', 'Saved successfully!'));
      form.resetFields();
      onSuccess();
    } catch (error) {
      console.error('Form submission failed', error);
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
        return <DynamicSelect field={field} />;
      case 'date':
        return <DatePicker style={{ width: '100%' }} />;
      case 'number':
        return <Input type="number" placeholder={t(field.labelKey)} />;
      case 'text':
      default:
        return <Input placeholder={t(field.labelKey)} />;
    }
  };

  const getRules = (field: any) => {
    const rules: any[] = [];
    if (field.required) {
      rules.push({ required: true, message: t('form.required', 'This field is required') });
    }
    if (field.minLength) {
      rules.push({ min: field.minLength, message: `Minimum ${field.minLength} characters required` });
    }
    if (field.maxLength) {
      rules.push({ max: field.maxLength, message: `Maximum ${field.maxLength} characters allowed` });
    }
    if (field.regexPattern) {
      rules.push({ pattern: new RegExp(field.regexPattern), message: 'Invalid format' });
    }
    return rules;
  };

  return (
    <Modal
      title={t(activeSchema?.titleKey || schema.titleKey)}
      open={open}
      onOk={handleSubmit}
      onCancel={onCancel}
      confirmLoading={loading}
      destroyOnHidden
    >
      <Form form={form} layout="vertical" preserve={false}>
        {schema.templateEndpoint && !selectedTemplate ? (
          <Form.Item label={t('forms.selectTemplate', 'Please select a template to continue:')}>
            <Select 
              style={{ width: '100%' }} 
              placeholder={t('forms.selectTemplatePlaceholder', 'Select Template')}
              loading={loading}
              onChange={handleTemplateChange}
            >
              {templates.map(tmpl => (
                <Select.Option key={tmpl.id} value={tmpl.id}>
                  {tmpl.localizedName?.translations?.en || tmpl.id}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
        ) : (
          activeSchema?.fields?.map((field) => (
            <Form.Item
              key={field.name}
              name={field.name}
              label={t(field.labelKey) || field.labelKey}
              rules={getRules(field)}
            >
              {renderField(field)}
            </Form.Item>
          ))
        )}
      </Form>
    </Modal>
  );
}

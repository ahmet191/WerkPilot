import React, { useState, useEffect } from 'react';
import { Modal, Form, Input, Select, DatePicker, message, Checkbox, Button } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
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
  const [fetchedOptions, setFetchedOptions] = useState<any[]>([]);
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
      // Smart mapping for different entity types
      const mappedOptions = res.data.map((item: any) => {
        let label = item.label || item.name || item.title || item.titleKey || item.id;
        if (item.localizedName?.translations) {
          label = item.localizedName.translations['en'] || label;
        }
        return {
          value: item.id || item.value,
          label: label
        };
      });
      setFetchedOptions(mappedOptions);
    } catch (err) {
      console.error(`Failed to load options for ${field.name}`, err);
    } finally {
      setLoading(false);
    }
  };

  const options = field.dataSourceEndpoint 
    ? fetchedOptions 
    : (field.options?.map((opt: any) => {
        if (typeof opt === 'string') {
          return { value: opt, label: opt };
        }
        return {
          value: opt.value !== undefined ? opt.value : (opt.id !== undefined ? opt.id : opt),
          label: opt.label !== undefined ? opt.label : (opt.name !== undefined ? opt.name : (opt.value !== undefined ? opt.value : opt))
        };
      }) || []);

  return (
    <Select placeholder={t(field.labelKey)} loading={loading} {...props}>
      {options.map((opt: any) => (
        <Select.Option key={opt.value} value={opt.value}>
          {t(opt.label)}
        </Select.Option>
      ))}
    </Select>
  );
};

const FieldsBuilder = ({ value = [], onChange }: any) => {
  const { t } = useTranslation();
  const [fields, setFields] = useState<any[]>(value);

  useEffect(() => {
    setFields(value);
  }, [value]);

  const updateFields = (newFields: any[]) => {
    setFields(newFields);
    if (onChange) {
      onChange(newFields);
    }
  };

  const addField = () => {
    updateFields([
      ...fields,
      {
        key: `field_${fields.length + 1}`,
        name: `Field ${fields.length + 1}`,
        type: 'Text',
        isRequired: false,
        dropdownOptions: []
      }
    ]);
  };

  const removeField = (index: number) => {
    updateFields(fields.filter((_, i) => i !== index));
  };

  const handleFieldChange = (index: number, key: string, val: any) => {
    const updated = fields.map((f, i) => {
      if (i === index) {
        return { ...f, [key]: val };
      }
      return f;
    });
    updateFields(updated);
  };

  return (
    <div style={{ border: '1px solid #f0f0f0', padding: '12px', borderRadius: '8px', background: '#fafafa', marginBottom: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <strong style={{ fontSize: '13px' }}>{t('templates.fields', 'Form Fields')}</strong>
        <Button type="dashed" size="small" icon={<PlusOutlined />} onClick={addField}>
          {t('templates.addField', 'Add Field')}
        </Button>
      </div>
      {fields.length === 0 ? (
        <div style={{ textAlign: 'center', color: '#8c8c8c', padding: '12px 0' }}>{t('templates.noFields', 'No fields added yet')}</div>
      ) : (
        fields.map((field, index) => (
          <div key={index} style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '8px', alignItems: 'center', background: '#fff', padding: '8px', borderRadius: '4px', border: '1px solid #e8e8e8' }}>
            <Input 
              size="small" 
              placeholder="Technical Key (e.g. title)" 
              value={field.key} 
              style={{ width: '130px' }}
              onChange={(e) => handleFieldChange(index, 'key', e.target.value)} 
            />
            <Input 
              size="small" 
              placeholder="Display Name" 
              value={field.name} 
              style={{ width: '130px' }}
              onChange={(e) => handleFieldChange(index, 'name', e.target.value)} 
            />
            <Select 
              size="small" 
              value={field.type} 
              style={{ width: '110px' }}
              onChange={(val) => handleFieldChange(index, 'type', val)}
            >
              <Select.Option value="Text">Text</Select.Option>
              <Select.Option value="Number">Number</Select.Option>
              <Select.Option value="Date">Date</Select.Option>
              <Select.Option value="Dropdown">Dropdown</Select.Option>
            </Select>
            <Checkbox 
              checked={field.isRequired}
              onChange={(e) => handleFieldChange(index, 'isRequired', e.target.checked)}
            >
              Req?
            </Checkbox>
            {field.type === 'Dropdown' && (
              <Input 
                size="small" 
                placeholder="Options (comma separated)" 
                value={field.dropdownOptionsRaw || (field.dropdownOptions || []).join(',')} 
                style={{ flex: '1 1 100%', marginTop: '4px' }}
                onChange={(e) => {
                  const raw = e.target.value;
                  const opts = raw.split(',').map((x: string) => x.trim()).filter(Boolean);
                  const updated = fields.map((f, i) => {
                    if (i === index) {
                      return { ...f, dropdownOptions: opts, dropdownOptionsRaw: raw };
                    }
                    return f;
                  });
                  updateFields(updated);
                }} 
              />
            )}
            <Button 
              type="text" 
              danger 
              size="small" 
              icon={<DeleteOutlined />} 
              onClick={() => removeField(index)} 
              style={{ marginLeft: 'auto' }}
            />
          </div>
        ))
      )}
    </div>
  );
};

const WorkflowBuilder = ({ value = { transitions: [] }, onChange }: any) => {
  const { t } = useTranslation();
  const [transitions, setTransitions] = useState<any[]>(value.transitions || []);

  useEffect(() => {
    setTransitions(value.transitions || []);
  }, [value]);

  const updateTransitions = (newTransitions: any[]) => {
    setTransitions(newTransitions);
    if (onChange) {
      onChange({ transitions: newTransitions });
    }
  };

  const addTransition = () => {
    updateTransitions([
      ...transitions,
      {
        fromState: 'New',
        toState: 'InProgress',
        trigger: 'StartWork',
        requiredRole: 'Agent'
      }
    ]);
  };

  const removeTransition = (index: number) => {
    updateTransitions(transitions.filter((_, i) => i !== index));
  };

  const handleTransitionChange = (index: number, key: string, val: any) => {
    const updated = transitions.map((t, i) => {
      if (i === index) {
        return { ...t, [key]: val };
      }
      return t;
    });
    updateTransitions(updated);
  };

  const states = ['New', 'InProgress', 'WaitingForApproval', 'Resolved', 'Closed'];
  const roles = ['Requester', 'Agent', 'Approver', 'Administrator'];

  return (
    <div style={{ border: '1px solid #f0f0f0', padding: '12px', borderRadius: '8px', background: '#fafafa', marginBottom: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <strong style={{ fontSize: '13px' }}>{t('templates.workflow', 'Workflow Transitions')}</strong>
        <Button type="dashed" size="small" icon={<PlusOutlined />} onClick={addTransition}>
          {t('templates.addTransition', 'Add Transition')}
        </Button>
      </div>
      {transitions.length === 0 ? (
        <div style={{ textAlign: 'center', color: '#8c8c8c', padding: '12px 0' }}>{t('templates.noTransitions', 'No transitions added yet')}</div>
      ) : (
        transitions.map((trans, index) => (
          <div key={index} style={{ display: 'flex', gap: '8px', marginBottom: '8px', alignItems: 'center', background: '#fff', padding: '8px', borderRadius: '4px', border: '1px solid #e8e8e8' }}>
            <Select 
              size="small" 
              value={trans.fromState} 
              style={{ width: '100px' }}
              onChange={(val) => handleTransitionChange(index, 'fromState', val)}
            >
              {states.map(s => <Select.Option key={s} value={s}>{s}</Select.Option>)}
            </Select>
            <span>➔</span>
            <Select 
              size="small" 
              value={trans.toState} 
              style={{ width: '100px' }}
              onChange={(val) => handleTransitionChange(index, 'toState', val)}
            >
              {states.map(s => <Select.Option key={s} value={s}>{s}</Select.Option>)}
            </Select>
            <Input 
              size="small" 
              placeholder="Trigger Name (e.g. Start)" 
              value={trans.trigger} 
              style={{ width: '100px' }}
              onChange={(e) => handleTransitionChange(index, 'trigger', e.target.value)} 
            />
            <Select 
              size="small" 
              value={trans.requiredRole || ''} 
              style={{ flex: 1, minWidth: '90px' }}
              onChange={(val) => handleTransitionChange(index, 'requiredRole', val || null)}
            >
              <Select.Option value="">Any Role</Select.Option>
              {roles.map(r => <Select.Option key={r} value={r}>{r}</Select.Option>)}
            </Select>
            <Button 
              type="text" 
              danger 
              size="small" 
              icon={<DeleteOutlined />} 
              onClick={() => removeTransition(index)} 
            />
          </div>
        ))
      )}
    </div>
  );
};

interface DynamicFormModalProps {
  open: boolean;
  schema: FormSchema;
  editRecord?: any;
  editEndpoint?: string | null;
  onCancel: () => void;
  onSuccess: () => void;
}

export function DynamicFormModal({ open, schema, editRecord, editEndpoint, onCancel, onSuccess }: DynamicFormModalProps) {
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
      if (editRecord) {
        form.setFieldsValue(editRecord);
      }
      if (schema.templateEndpoint && !editRecord) {
        loadTemplates();
      } else {
        setActiveSchema(schema);
      }
    }
  }, [open, schema, editRecord, form]);

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
      
      // Parse JSON fields
      activeSchema?.fields?.forEach((field: any) => {
        if (field.type === 'json' && values[field.name]) {
          try {
            values[field.name] = JSON.parse(values[field.name]);
          } catch (e) {
            message.error(`Invalid JSON in ${field.labelKey}`);
            throw new Error('Invalid JSON');
          }
        }
      });

      // Format payload for /templates if it's templates creation
      let payload = values;
      const submitUrl = activeSchema.submitEndpoint || schema.submitEndpoint;
      if (submitUrl === '/templates') {
        const formattedFields = (values.fields || []).map((f: any) => ({
          key: f.key,
          localizedName: {
            translations: {
              en: f.name,
              tr: f.name
            }
          },
          type: f.type,
          isRequired: f.isRequired,
          validationRegex: null,
          dropdownOptions: f.type === 'Dropdown' ? (f.dropdownOptions || []) : null,
          localizedDescription: {
            translations: {
              en: "",
              tr: ""
            }
          }
        }));

        const formattedTransitions = (values.workflow?.transitions || []).map((t: any) => ({
          fromState: t.fromState,
          toState: t.toState,
          trigger: t.trigger,
          requiredRole: t.requiredRole || null
        }));

        payload = {
          localizedName: {
            translations: {
              en: values.templateName,
              tr: values.templateName
            }
          },
          serviceDeskId: values.serviceDeskId,
          fields: formattedFields,
          workflow: { transitions: formattedTransitions }
        };
      } else if (schema.templateEndpoint) {
        // If dynamic ticket form, nest values in DynamicData except templateId/requesterId
        payload = {
          templateId: selectedTemplate,
          requesterId: values.requesterId || "11111111-1111-1111-1111-111111111111", // Mock requester fallback
          dynamicData: values
        };
      }

      setLoading(true);
      if (editRecord && editEndpoint) {
        await api.put(editEndpoint, payload);
      } else {
        await SchemaService.submitFormData(submitUrl, payload);
      }
      
      message.success(t('form.success', 'Saved successfully!'));
      form.resetFields();
      onSuccess();
    } catch (error: any) {
      console.error('Form submission failed', error);
      if (error && error.message !== 'Invalid JSON') {
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
      case 'fields_builder':
        return <FieldsBuilder />;
      case 'workflow_builder':
        return <WorkflowBuilder />;
      case 'json':
        return <Input.TextArea rows={6} placeholder="Enter JSON..." style={{ fontFamily: 'monospace' }} />;
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
      rules.push({
        validator: (_: any, value: any) => {
          if (!value || new RegExp(field.regexPattern).test(value)) {
            return Promise.resolve();
          }
          const isTr = t('form.invalidFormat', 'Invalid format') === 'Geçersiz format';
          const expectedExample = field.regexPattern === '^[A-Z]{3}-\\d{4}$'
            ? ` (${isTr ? 'örn.' : 'e.g.'} ABC-1234)`
            : '';
          return Promise.reject(new Error(`${t('form.invalidFormat', 'Invalid format')}${expectedExample}`));
        }
      });
    }
    return rules;
  };

  return (
    <Modal
      title={editRecord ? t('form.edit', 'Edit') : t(activeSchema?.titleKey || schema.titleKey)}
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

import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Form, Input, Modal, Select, Space, Tag, Typography, message } from 'antd';
import { TranslationOutlined, EditOutlined, UndoOutlined } from '@ant-design/icons';
import { EnterpriseDataGrid, type GridFetchParams, type GridToolbarAction } from '../../shared/components/datagrid/EnterpriseDataGrid';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text } = Typography;

interface TranslationRow {
  /** The grid's generic expects an id; the translation key doubles as one. */
  id: string;
  key: string;
  defaultValue: string;
  overrideValue: string | null;
  isCustomized: boolean;
}

/**
 * Lets a tenant reword any interface string without a deploy. The platform's
 * shipped text stays the default; only what the tenant changes is stored.
 */
export const TranslationsAdminPage: React.FC = () => {
  const { t, language: uiLanguage, reloadTranslations } = useTranslation();
  // The UI language may be a full locale ("en-US"); the editor works in the
  // base code the API stores against.
  const [language, setLanguage] = useState<string>((uiLanguage ?? 'en').split('-')[0]);
  const [languages, setLanguages] = useState<string[]>(['en', 'tr']);
  const [rows, setRows] = useState<TranslationRow[]>([]);
  const [refreshKey, setRefreshKey] = useState(0);
  const [editing, setEditing] = useState<TranslationRow | null>(null);
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    const res = await api.get(`/localization/admin/${language}`);
    setLanguages(res.data.languages ?? ['en']);
    const items: TranslationRow[] = (res.data.items ?? []).map((i: Omit<TranslationRow, 'id'>) => ({ ...i, id: i.key }));
    setRows(items);
    return items;
  }, [language]);

  useEffect(() => { load().catch(() => undefined); }, [load, refreshKey]);

  // The grid is fed from the already-loaded list: the catalog is small and
  // filtering client-side keeps the editor instant.
  const fetchData = useCallback(async ({ page, pageSize, search, filters }: GridFetchParams) => {
    let data = rows;
    if (search) {
      const needle = search.toLowerCase();
      data = data.filter(r =>
        r.key.toLowerCase().includes(needle) ||
        r.defaultValue.toLowerCase().includes(needle) ||
        (r.overrideValue ?? '').toLowerCase().includes(needle));
    }
    for (const [field, value] of Object.entries(filters)) {
      const needle = value.toLowerCase();
      data = data.filter(r => String((r as any)[field] ?? '').toLowerCase().includes(needle));
    }
    const start = (page - 1) * pageSize;
    return { data: data.slice(start, start + pageSize), total: data.length };
  }, [rows]);

  const openEditor = (row: TranslationRow) => {
    setEditing(row);
    form.setFieldsValue({ value: row.overrideValue ?? row.defaultValue });
  };

  const saveOverride = async () => {
    const values = await form.validateFields();
    if (!editing) return;
    setSaving(true);
    try {
      await api.put(`/localization/admin/${language}/${encodeURIComponent(editing.key)}`, { value: values.value });
      message.success(t('translations.saved', 'Text updated'));
      setEditing(null);
      setRefreshKey(k => k + 1);
      reloadTranslations?.();
    } catch {
      // toast handled globally
    } finally {
      setSaving(false);
    }
  };

  const revert = async (row: TranslationRow) => {
    await api.delete(`/localization/admin/${language}/${encodeURIComponent(row.key)}`);
    message.success(t('translations.reverted', 'Reverted to the default text'));
    setRefreshKey(k => k + 1);
    reloadTranslations?.();
  };

  const toolbarActions: GridToolbarAction[] = [
    {
      key: 'edit',
      label: t('translations.edit', 'Customize'),
      icon: <EditOutlined />,
      type: 'primary',
      requiresSelection: 'single',
      onClick: r => openEditor(r[0]),
    },
    {
      key: 'revert',
      label: t('translations.revert', 'Revert'),
      icon: <UndoOutlined />,
      requiresSelection: 'single',
      onClick: r => {
        if (!r[0].isCustomized) {
          message.info(t('translations.notCustomized', 'This text is not customized'));
          return;
        }
        revert(r[0]);
      },
    },
  ];

  const customizedCount = rows.filter(r => r.isCustomized).length;

  return (
    <div>
      <Space align="center" style={{ marginBottom: 18 }}>
        <TranslationOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
        <Title level={3} style={{ margin: 0 }}>{t('translations.title', 'Interface text')}</Title>
      </Space>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 14 }}
        message={t('translations.help', 'Reword any label to match your organization. Anything you do not customize keeps the shipped wording.')}
        action={
          <Space>
            <Text type="secondary">{customizedCount} {t('translations.customized', 'customized')}</Text>
            <Select
              value={language}
              onChange={setLanguage}
              options={languages.map(l => ({ value: l, label: l.toUpperCase() }))}
              style={{ width: 90 }}
            />
          </Space>
        }
      />

      <EnterpriseDataGrid<TranslationRow>
        title={t('translations.grid', 'Translations')}
        rowKey="key"
        columns={[
          { title: t('translations.key', 'Key'), dataIndex: 'key', key: 'key', width: 260, sorter: true },
          { title: t('translations.default', 'Default'), dataIndex: 'defaultValue', key: 'defaultValue' },
          {
            title: t('translations.override', 'Your text'), dataIndex: 'overrideValue', key: 'overrideValue',
            render: (v: string | null, row: TranslationRow) => row.isCustomized
              ? <Space><span>{v}</span><Tag color="blue">{t('translations.custom', 'custom')}</Tag></Space>
              : <Text type="secondary">—</Text>,
          },
        ]}
        filterableColumns={['key', 'defaultValue', 'overrideValue']}
        fetchData={fetchData}
        toolbarActions={toolbarActions}
        onRowDoubleClick={openEditor}
        exportable
      />

      <Modal
        open={editing !== null}
        title={editing?.key}
        onCancel={() => setEditing(null)}
        onOk={saveOverride}
        confirmLoading={saving}
        destroyOnHidden
      >
        <Form form={form} layout="vertical">
          <Alert
            type="info"
            style={{ marginBottom: 12 }}
            message={<Text type="secondary" style={{ fontSize: 12 }}>{t('translations.default', 'Default')}: {editing?.defaultValue}</Text>}
          />
          <Form.Item
            name="value"
            label={t('translations.yourText', 'Your text')}
            rules={[{ required: true, message: t('translations.valueRequired', 'Enter the text to display') }]}
          >
            <Input.TextArea rows={3} maxLength={2000} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

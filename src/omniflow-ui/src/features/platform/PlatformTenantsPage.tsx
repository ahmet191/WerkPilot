import React, { useCallback, useState } from 'react';
import { Alert, Form, Input, Modal, Space, Tag, Typography, message } from 'antd';
import { CloudServerOutlined, StopOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { EnterpriseDataGrid, type GridFetchParams, type GridToolbarAction } from '../../shared/components/datagrid/EnterpriseDataGrid';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title } = Typography;

interface TenantRow {
  id: string;
  name: string;
  domain: string;
  isActive: boolean;
  createdAt: string;
  userCount: number;
  ticketCount: number;
}

/** Platform operator console: provision and suspend customer workspaces. */
export const PlatformTenantsPage: React.FC = () => {
  const { t } = useTranslation();
  const [refreshKey, setRefreshKey] = useState(0);
  const [createOpen, setCreateOpen] = useState(false);

  const fetchData = useCallback(async ({ page, pageSize, search }: GridFetchParams) => {
    const res = await api.get('/platform/tenants');
    let rows: TenantRow[] = res.data ?? [];
    if (search) {
      const needle = search.toLowerCase();
      rows = rows.filter(r => r.name.toLowerCase().includes(needle) || r.domain.toLowerCase().includes(needle));
    }
    // The tenant registry is small; page it client-side.
    const start = (page - 1) * pageSize;
    return { data: rows.slice(start, start + pageSize), total: rows.length };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshKey]);

  const setActive = async (row: TenantRow, active: boolean) => {
    await api.post(`/platform/tenants/${row.id}/${active ? 'activate' : 'deactivate'}`);
    message.success(active
      ? t('platform.activated', 'Workspace activated')
      : t('platform.deactivated', 'Workspace suspended'));
    setRefreshKey(k => k + 1);
  };

  const toolbarActions: GridToolbarAction[] = [
    {
      key: 'activate',
      label: t('platform.activate', 'Activate'),
      icon: <CheckCircleOutlined />,
      requiresSelection: 'single',
      onClick: rows => setActive(rows[0], true),
    },
    {
      key: 'deactivate',
      label: t('platform.deactivate', 'Suspend'),
      icon: <StopOutlined />,
      danger: true,
      requiresSelection: 'single',
      onClick: rows => Modal.confirm({
        title: t('platform.deactivateConfirm', 'Suspend this workspace?'),
        content: t('platform.deactivateHint', 'Its users will not be able to sign in until it is activated again.'),
        okButtonProps: { danger: true },
        onOk: () => setActive(rows[0], false),
      }),
    },
  ];

  return (
    <div>
      <Space align="center" style={{ marginBottom: 18 }}>
        <CloudServerOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
        <Title level={3} style={{ margin: 0 }}>{t('platform.title', 'Workspaces')}</Title>
      </Space>

      <EnterpriseDataGrid<TenantRow>
        title={t('platform.tenants', 'Tenants')}
        rowKey="id"
        columns={[
          { title: t('platform.name', 'Name'), dataIndex: 'name', key: 'name', sorter: true },
          { title: t('platform.domain', 'Domain'), dataIndex: 'domain', key: 'domain' },
          {
            title: t('platform.status', 'Status'), dataIndex: 'isActive', key: 'isActive', width: 120,
            render: (active: boolean) => (
              <Tag color={active ? 'green' : 'red'}>
                {active ? t('platform.active', 'Active') : t('platform.suspended', 'Suspended')}
              </Tag>
            ),
          },
          { title: t('platform.users', 'Users'), dataIndex: 'userCount', key: 'userCount', width: 100 },
          { title: t('platform.tickets', 'Tickets'), dataIndex: 'ticketCount', key: 'ticketCount', width: 100 },
          {
            title: t('platform.created', 'Created'), dataIndex: 'createdAt', key: 'createdAt', width: 180,
            render: (v: string) => new Date(v).toLocaleDateString(),
          },
        ]}
        fetchData={fetchData}
        toolbarActions={toolbarActions}
        onAdd={() => setCreateOpen(true)}
        exportable
      />

      <CreateTenantModal
        open={createOpen}
        onClose={created => { setCreateOpen(false); if (created) setRefreshKey(k => k + 1); }}
      />
    </div>
  );
};

const CreateTenantModal: React.FC<{ open: boolean; onClose: (created: boolean) => void }> = ({ open, onClose }) => {
  const { t } = useTranslation();
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);

  const submit = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      await api.post('/platform/tenants', values);
      message.success(t('platform.createdMsg', 'Workspace created — the administrator has been invited by email'));
      onClose(true);
    } catch {
      // toast handled globally
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open={open}
      title={t('platform.newTitle', 'New workspace')}
      onCancel={() => onClose(false)}
      onOk={submit}
      confirmLoading={saving}
      destroyOnHidden
      afterOpenChange={visible => { if (visible) form.resetFields(); }}
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message={t('platform.newHint',
          'The workspace starts with its own service desk, ticket template, SLA targets, navigation and permission matrix. Its administrator receives an invitation to set a password.')}
      />
      <Form form={form} layout="vertical">
        <Form.Item name="name" label={t('platform.name', 'Name')} rules={[{ required: true, min: 2 }]}>
          <Input placeholder="Acme Corporation" />
        </Form.Item>
        <Form.Item name="domain" label={t('platform.domain', 'Domain')} rules={[{ required: true, min: 3 }]}>
          <Input placeholder="acme.werkpilot.local" />
        </Form.Item>
        <Form.Item name="adminName" label={t('platform.adminName', 'Administrator name')} rules={[{ required: true, min: 2 }]}>
          <Input placeholder="Jane Doe" />
        </Form.Item>
        <Form.Item name="adminEmail" label={t('platform.adminEmail', 'Administrator email')} rules={[{ required: true, type: 'email' }]}>
          <Input placeholder="jane@acme.com" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

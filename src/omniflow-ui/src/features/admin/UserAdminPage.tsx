import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert, Button, Descriptions, Drawer, Form, Input, message, Modal, Select, Space, Switch, Tag, Divider, Popconfirm,
} from 'antd';
import {
  UserAddOutlined, EditOutlined, DeleteOutlined, IdcardOutlined, UnlockOutlined,
  StopOutlined, CheckCircleOutlined, MailOutlined, KeyOutlined, SafetyOutlined,
} from '@ant-design/icons';
import { EnterpriseDataGrid, type GridFetchParams, type GridToolbarAction } from '../../shared/components/datagrid/EnterpriseDataGrid';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

interface UserRow {
  id: string;
  name: string;
  email: string;
  role: string;
  departmentName?: string;
}

interface AccountStatus {
  hasAccount: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  lockedOut: boolean;
  lockoutEnd?: string | null;
  disabled: boolean;
  accessFailedCount: number;
  roles: string[];
}

const DOMAIN_ROLES = ['Requester', 'Agent', 'Approver', 'Administrator'];
const IDENTITY_ROLES = ['Administrator', 'HRAdmin', 'ITAgent', 'Requester'];

/** Administrator console: domain users + their login accounts (lockout, roles, MFA, passwords). */
export const UserAdminPage: React.FC = () => {
  const { t } = useTranslation();
  const [refreshKey, setRefreshKey] = useState(0);
  const [departments, setDepartments] = useState<{ label: string; value: string }[]>([]);

  const [formOpen, setFormOpen] = useState(false);
  const [editRecord, setEditRecord] = useState<UserRow | null>(null);
  const [accountUser, setAccountUser] = useState<UserRow | null>(null);

  useEffect(() => {
    api.get('/departments?pageSize=100').then(res => {
      const items = res.data?.items ?? res.data ?? [];
      setDepartments(items.map((d: any) => ({
        value: d.id,
        label: d.localizedName?.translations?.en ?? d.name ?? d.id,
      })));
    }).catch(() => setDepartments([]));
  }, []);

  const fetchData = useCallback(async ({ page, pageSize, search, sortBy, sortDesc, filters }: GridFetchParams) => {
    const params = new URLSearchParams();
    params.set('current', String(page));
    params.set('pageSize', String(pageSize));
    if (search) params.set('search', search);
    if (sortBy) { params.set('sortBy', sortBy); params.set('sortDesc', String(!!sortDesc)); }
    Object.entries(filters).forEach(([field, value]) => params.set(`filters[${field}]`, value));
    const res = await api.get(`/users?${params.toString()}`);
    if (res.data?.items !== undefined) return { data: res.data.items, total: res.data.totalCount };
    return { data: res.data ?? [], total: (res.data ?? []).length };
    // refreshKey is a dependency so mutations refetch the grid.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshKey]);

  const deleteUser = (row: UserRow) => {
    Modal.confirm({
      title: t('admin.users.deleteConfirm', 'Delete this user?'),
      content: `${row.name} (${row.email})`,
      okButtonProps: { danger: true },
      onOk: async () => {
        await api.delete(`/users/${row.id}`);
        message.success(t('admin.users.deleted', 'User deleted and login disabled'));
        setRefreshKey(k => k + 1);
      },
    });
  };

  const toolbarActions: GridToolbarAction[] = [
    {
      key: 'edit',
      label: t('grid.edit', 'Edit'),
      icon: <EditOutlined />,
      requiresSelection: 'single',
      onClick: rows => { setEditRecord(rows[0]); setFormOpen(true); },
    },
    {
      key: 'account',
      label: t('admin.users.account', 'Account'),
      icon: <IdcardOutlined />,
      type: 'primary',
      requiresSelection: 'single',
      onClick: rows => setAccountUser(rows[0]),
    },
    {
      key: 'delete',
      label: t('grid.delete', 'Delete'),
      icon: <DeleteOutlined />,
      danger: true,
      requiresSelection: 'single',
      onClick: rows => deleteUser(rows[0]),
    },
  ];

  return (
    <>
      <EnterpriseDataGrid<UserRow>
        title={t('admin.users.title', 'User Management')}
        rowKey="id"
        columns={[
          { title: t('users.name', 'Name'), dataIndex: 'name', key: 'name', sorter: true },
          { title: t('users.email', 'Email'), dataIndex: 'email', key: 'email', sorter: true },
          {
            title: t('users.role', 'Role'), dataIndex: 'role', key: 'role', sorter: true, width: 140,
            render: (v: string) => <Tag color={v === 'Administrator' ? 'gold' : v === 'Agent' ? 'blue' : 'default'}>{v}</Tag>,
          },
          { title: t('users.department', 'Department'), dataIndex: 'departmentName', key: 'departmentName', width: 180 },
        ]}
        filterableColumns={['name', 'email', 'role']}
        groupableColumns={[{ key: 'role', title: t('users.role', 'Role') }, { key: 'departmentName', title: t('users.department', 'Department') }]}
        fetchData={fetchData}
        toolbarActions={toolbarActions}
        onAdd={() => { setEditRecord(null); setFormOpen(true); }}
        onRowDoubleClick={row => setAccountUser(row)}
        exportable
      />

      <UserFormModal
        open={formOpen}
        record={editRecord}
        departments={departments}
        onClose={saved => { setFormOpen(false); if (saved) setRefreshKey(k => k + 1); }}
      />

      <AccountDrawer
        user={accountUser}
        onClose={() => setAccountUser(null)}
      />
    </>
  );
};

/** Create/edit a domain user; create also provisions the login account. */
const UserFormModal: React.FC<{
  open: boolean;
  record: UserRow | null;
  departments: { label: string; value: string }[];
  onClose: (saved: boolean) => void;
}> = ({ open, record, departments, onClose }) => {
  const { t } = useTranslation();
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const isEdit = record !== null;

  const submit = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      if (isEdit) {
        await api.put(`/users/${record!.id}`, { id: record!.id, ...values });
        message.success(t('admin.users.updated', 'User updated'));
      } else {
        await api.post('/users', values);
        message.success(values.sendInvite
          ? t('admin.users.createdInvited', 'User created — invitation email sent')
          : t('admin.users.created', 'User created'));
      }
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
      title={isEdit ? t('admin.users.editTitle', 'Edit user') : t('admin.users.newTitle', 'New user')}
      onCancel={() => onClose(false)}
      onOk={submit}
      confirmLoading={saving}
      destroyOnHidden
      afterOpenChange={visible => {
        if (visible) {
          form.resetFields();
          if (record) form.setFieldsValue(record);
          else form.setFieldsValue({ role: 'Requester', sendInvite: true });
        }
      }}
    >
      <Form form={form} layout="vertical">
        <Form.Item name="name" label={t('users.name', 'Name')} rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="email" label={t('users.email', 'Email')} rules={[{ required: true, type: 'email' }]}>
          <Input />
        </Form.Item>
        <Form.Item name="role" label={t('users.role', 'Role')} rules={[{ required: true }]}>
          <Select options={DOMAIN_ROLES.map(r => ({ value: r, label: r }))} />
        </Form.Item>
        <Form.Item name="departmentId" label={t('users.department', 'Department')} rules={[{ required: true }]}>
          <Select options={departments} showSearch optionFilterProp="label" />
        </Form.Item>
        {!isEdit && (
          <>
            <Divider plain style={{ margin: '8px 0' }}>{t('admin.users.loginSection', 'Login account')}</Divider>
            <Form.Item
              name="initialPassword"
              label={t('admin.users.initialPassword', 'Initial password (optional)')}
              extra={t('admin.users.initialPasswordHint', 'Leave empty to let the user set their own password via the invitation email.')}
            >
              <Input.Password autoComplete="new-password" />
            </Form.Item>
            <Form.Item name="sendInvite" label={t('admin.users.sendInvite', 'Send invitation email')} valuePropName="checked">
              <Switch />
            </Form.Item>
          </>
        )}
      </Form>
    </Modal>
  );
};

/** Login-account status + recovery actions for one user. */
const AccountDrawer: React.FC<{ user: UserRow | null; onClose: () => void }> = ({ user, onClose }) => {
  const { t } = useTranslation();
  const [status, setStatus] = useState<AccountStatus | null>(null);
  const [roles, setRoles] = useState<string[]>([]);
  const [tempPassword, setTempPassword] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    if (!user) return;
    setStatus(null);
    const res = await api.get(`/admin/users/${user.id}/account`);
    setStatus(res.data);
    setRoles(res.data.roles ?? []);
  }, [user]);

  useEffect(() => { load().catch(() => undefined); }, [load]);

  const act = async (fn: () => Promise<unknown>, successMsg: string) => {
    setBusy(true);
    try {
      await fn();
      message.success(successMsg);
      await load();
    } catch {
      // toast handled globally
    } finally {
      setBusy(false);
    }
  };

  const statusTags = status?.hasAccount ? (
    <Space wrap>
      {status.disabled
        ? <Tag color="red">{t('admin.account.disabled', 'Disabled')}</Tag>
        : <Tag color="green">{t('admin.account.active', 'Active')}</Tag>}
      {status.lockedOut && <Tag color="orange">{t('admin.account.locked', 'Locked out')}</Tag>}
      {status.twoFactorEnabled && <Tag color="blue">MFA</Tag>}
    </Space>
  ) : null;

  return (
    <Drawer
      open={user !== null}
      onClose={onClose}
      width={480}
      title={user ? `${user.name} — ${t('admin.users.account', 'Account')}` : ''}
      destroyOnHidden
    >
      {user && status === null && <Alert type="info" message={t('common.loading', 'Loading…')} />}

      {user && status && !status.hasAccount && (
        <Space direction="vertical" style={{ width: '100%' }} size={16}>
          <Alert
            type="warning"
            showIcon
            message={t('admin.account.none', 'No login account')}
            description={t('admin.account.noneHint', 'This user exists in the directory but cannot sign in yet.')}
          />
          <Button
            type="primary"
            icon={<UserAddOutlined />}
            loading={busy}
            onClick={() => act(
              () => api.post(`/admin/users/${user.id}/provision-account`, { email: user.email, sendInvite: true, role: 'Requester' }),
              t('admin.account.provisioned', 'Login account created — invitation sent'))}
          >
            {t('admin.account.provision', 'Create login account & send invite')}
          </Button>
        </Space>
      )}

      {user && status?.hasAccount && (
        <Space direction="vertical" style={{ width: '100%' }} size={18}>
          <Descriptions column={1} size="small" bordered>
            <Descriptions.Item label={t('admin.account.status', 'Status')}>{statusTags}</Descriptions.Item>
            <Descriptions.Item label={t('admin.account.failedAttempts', 'Failed attempts')}>
              {status.accessFailedCount}
            </Descriptions.Item>
            {status.lockedOut && status.lockoutEnd && (
              <Descriptions.Item label={t('admin.account.lockedUntil', 'Locked until')}>
                {new Date(status.lockoutEnd).toLocaleString()}
              </Descriptions.Item>
            )}
          </Descriptions>

          <Space wrap>
            {status.lockedOut && (
              <Button icon={<UnlockOutlined />} loading={busy}
                onClick={() => act(() => api.post(`/admin/users/${user.id}/unlock`), t('admin.account.unlocked', 'Account unlocked'))}>
                {t('admin.account.unlock', 'Unlock')}
              </Button>
            )}
            {status.disabled ? (
              <Button icon={<CheckCircleOutlined />} loading={busy}
                onClick={() => act(() => api.post(`/admin/users/${user.id}/enable`), t('admin.account.enabled', 'Account enabled'))}>
                {t('admin.account.enable', 'Enable')}
              </Button>
            ) : (
              <Popconfirm
                title={t('admin.account.disableConfirm', 'Disable sign-in for this user?')}
                onConfirm={() => act(() => api.post(`/admin/users/${user.id}/disable`), t('admin.account.disabledMsg', 'Account disabled'))}
              >
                <Button danger icon={<StopOutlined />} loading={busy}>{t('admin.account.disable', 'Disable')}</Button>
              </Popconfirm>
            )}
            {status.twoFactorEnabled && (
              <Popconfirm
                title={t('admin.account.resetMfaConfirm', 'Remove this user’s MFA (lost device)?')}
                onConfirm={() => act(() => api.post(`/admin/users/${user.id}/reset-mfa`), t('admin.account.mfaReset', 'MFA removed'))}
              >
                <Button icon={<SafetyOutlined />} loading={busy}>{t('admin.account.resetMfa', 'Reset MFA')}</Button>
              </Popconfirm>
            )}
            <Button icon={<MailOutlined />} loading={busy}
              onClick={() => act(() => api.post(`/admin/users/${user.id}/send-reset-email`), t('admin.account.resetSent', 'Reset email sent'))}>
              {t('admin.account.sendReset', 'Send reset email')}
            </Button>
          </Space>

          <Divider plain style={{ margin: '4px 0' }}>{t('admin.account.tempPassword', 'Set temporary password')}</Divider>
          <Space.Compact style={{ width: '100%' }}>
            <Input.Password
              prefix={<KeyOutlined />}
              placeholder={t('admin.account.newPassword', 'New password')}
              value={tempPassword}
              onChange={e => setTempPassword(e.target.value)}
              autoComplete="new-password"
            />
            <Button
              loading={busy}
              disabled={tempPassword.length < 8}
              onClick={() => act(async () => {
                await api.post(`/admin/users/${user.id}/reset-password`, { newPassword: tempPassword });
                setTempPassword('');
              }, t('admin.account.passwordSet', 'Password updated'))}
            >
              {t('common.apply', 'Apply')}
            </Button>
          </Space.Compact>

          <Divider plain style={{ margin: '4px 0' }}>{t('admin.account.apiRoles', 'API roles')}</Divider>
          <Space.Compact style={{ width: '100%' }}>
            <Select
              mode="multiple"
              style={{ width: '100%' }}
              value={roles}
              onChange={setRoles}
              options={IDENTITY_ROLES.map(r => ({ value: r, label: r }))}
            />
            <Button
              loading={busy}
              onClick={() => act(() => api.put(`/admin/users/${user.id}/roles`, { roles }), t('admin.account.rolesSaved', 'Roles updated'))}
            >
              {t('common.save', 'Save')}
            </Button>
          </Space.Compact>
        </Space>
      )}
    </Drawer>
  );
};

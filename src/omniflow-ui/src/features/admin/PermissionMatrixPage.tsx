import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Checkbox, Space, Table, Tabs, Tag, Typography, message } from 'antd';
import { SafetyCertificateOutlined, SaveOutlined, ReloadOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { usePermissions } from '../../core/PermissionContext';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text } = Typography;

interface PermissionDef {
  key: string;
  module: string;
  displayName: string;
}

interface MatrixResponse {
  superRole: string;
  roles: string[];
  matrix: Record<string, string[]>;
}

/**
 * Role × permission editor for the current tenant. The super role is shown
 * read-only: it always holds everything, which is what keeps a misconfiguration
 * from locking every administrator out.
 */
export const PermissionMatrixPage: React.FC = () => {
  const { t } = useTranslation();
  const { refresh: refreshMyPermissions } = usePermissions();

  const [catalog, setCatalog] = useState<PermissionDef[]>([]);
  const [data, setData] = useState<MatrixResponse | null>(null);
  const [draft, setDraft] = useState<Record<string, Set<string>>>({});
  const [savingRole, setSavingRole] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [catalogRes, matrixRes] = await Promise.all([
        api.get('/permissions/catalog'),
        api.get('/permissions/matrix'),
      ]);
      setCatalog(catalogRes.data ?? []);
      const matrix: MatrixResponse = matrixRes.data;
      setData(matrix);
      const next: Record<string, Set<string>> = {};
      for (const role of matrix.roles) {
        next[role] = new Set(matrix.matrix[role] ?? []);
      }
      setDraft(next);
    } catch {
      // toast handled globally
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const modules = useMemo(() => {
    const grouped = new Map<string, PermissionDef[]>();
    for (const perm of catalog) {
      const list = grouped.get(perm.module) ?? [];
      list.push(perm);
      grouped.set(perm.module, list);
    }
    return [...grouped.entries()];
  }, [catalog]);

  const toggle = (role: string, permission: string, checked: boolean) => {
    setDraft(prev => {
      const set = new Set(prev[role] ?? []);
      if (checked) set.add(permission); else set.delete(permission);
      return { ...prev, [role]: set };
    });
  };

  const saveRole = async (role: string) => {
    setSavingRole(role);
    try {
      await api.put(`/permissions/matrix/${role}`, { permissions: [...(draft[role] ?? [])] });
      message.success(t('permissions.saved', 'Permissions updated for {0}').replace('{0}', role));
      await load();
      await refreshMyPermissions();
    } catch {
      // toast handled globally
    } finally {
      setSavingRole(null);
    }
  };

  const isDirty = (role: string) => {
    const original = new Set(data?.matrix[role] ?? []);
    const current = draft[role] ?? new Set<string>();
    if (original.size !== current.size) return true;
    for (const key of current) if (!original.has(key)) return true;
    return false;
  };

  const roles = data?.roles ?? [];

  return (
    <div>
      <Space align="center" style={{ marginBottom: 18 }}>
        <SafetyCertificateOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
        <Title level={3} style={{ margin: 0 }}>{t('permissions.title', 'Roles & Permissions')}</Title>
      </Space>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 18 }}
        message={t('permissions.superRoleNote', 'Administrator always holds every permission')}
        description={t('permissions.superRoleHint',
          'This guarantees you can never lock yourself out of the platform. Configure the other roles below; changes apply on each user\'s next request.')}
      />

      <Tabs
        items={roles.map(role => ({
          key: role,
          label: (
            <Space size={6}>
              {role}
              {isDirty(role) && <Tag color="orange" style={{ marginInlineEnd: 0 }}>{t('common.unsaved', 'unsaved')}</Tag>}
            </Space>
          ),
          children: (
            <Space direction="vertical" style={{ width: '100%' }} size={16}>
              <Space>
                <Button
                  type="primary"
                  icon={<SaveOutlined />}
                  loading={savingRole === role}
                  disabled={!isDirty(role)}
                  onClick={() => saveRole(role)}
                >
                  {t('common.save', 'Save')}
                </Button>
                <Button icon={<ReloadOutlined />} onClick={load} disabled={loading}>
                  {t('grid.refresh', 'Refresh')}
                </Button>
                <Text type="secondary">
                  {(draft[role]?.size ?? 0)} / {catalog.length} {t('permissions.granted', 'granted')}
                </Text>
              </Space>

              {modules.map(([moduleName, perms]) => (
                <Card key={moduleName} size="small" title={moduleName} className="card-lift">
                  <Table
                    size="small"
                    rowKey="key"
                    pagination={false}
                    showHeader={false}
                    dataSource={perms}
                    columns={[
                      {
                        title: '',
                        dataIndex: 'displayName',
                        render: (text: string, record: PermissionDef) => (
                          <Space>
                            <Checkbox
                              checked={draft[role]?.has(record.key) ?? false}
                              onChange={e => toggle(role, record.key, e.target.checked)}
                            />
                            <span>{text}</span>
                            <Text type="secondary" style={{ fontSize: 12 }}>{record.key}</Text>
                          </Space>
                        ),
                      },
                    ]}
                  />
                </Card>
              ))}
            </Space>
          ),
        }))}
      />
    </div>
  );
};

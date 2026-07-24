import React, { useEffect, useState } from 'react';
import { Card, Table, Switch, Tag, message, Spin, Alert, Select, Space } from 'antd';
import { EyeOutlined, LockOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

interface VisibilityMatrixItem {
  id: string;
  roleName: string;
  serviceDeskKey: string;
  isVisible: boolean;
  canCreateTickets: boolean;
  isConfidential: boolean;
}

export const VisibilityMatrixPage: React.FC = () => {
  const { t } = useTranslation();
  const [matrix, setMatrix] = useState<VisibilityMatrixItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedRole, setSelectedRole] = useState('Requester');

  const serviceDeskKeys = ['IT', 'HR', 'Finance', 'Legal', 'Facilities'];
  const roles = ['Administrator', 'HRAdmin', 'ITAgent', 'Requester'];

  const fetchMatrix = async () => {
    setLoading(true);
    try {
      const res = await api.get('/visibility/matrix');
      setMatrix(res.data);
    } catch {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMatrix();
  }, []);

  const handleToggle = async (roleName: string, serviceDeskKey: string, field: string, currentValue: boolean) => {
    try {
      const existing = matrix.find(m => m.roleName === roleName && m.serviceDeskKey === serviceDeskKey) || {
        roleName,
        serviceDeskKey,
        isVisible: true,
        canCreateTickets: true,
        isConfidential: serviceDeskKey === 'HR' || serviceDeskKey === 'Legal'
      };

      const updated = {
        ...existing,
        [field]: !currentValue
      };

      await api.put('/visibility/matrix', updated);
      message.success(t('form.success', 'Visibility rule updated successfully!'));
      fetchMatrix();
    } catch {
      message.error(t('form.error', 'Failed to update visibility rule.'));
    }
  };

  const getRuleValue = (roleName: string, key: string, field: 'isVisible' | 'canCreateTickets' | 'isConfidential') => {
    const item = matrix.find(m => m.roleName === roleName && m.serviceDeskKey.toUpperCase() === key.toUpperCase());
    if (!item) {
      if (field === 'isConfidential') return key === 'HR' || key === 'Legal';
      return true;
    }
    return item[field];
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <EyeOutlined style={{ marginRight: 8, color: '#1890ff' }} />
              Department & Service Desk Visibility Matrix
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Configure per-role visibility and creation rights across IT, HR, Finance, Legal, and Facilities desks.
            </div>
          </div>
          <Select
            value={selectedRole}
            onChange={setSelectedRole}
            options={roles.map(r => ({ value: r, label: `Role: ${r}` }))}
            style={{ width: 200 }}
          />
        </div>

        <Alert
          message="Strict HR Confidentiality & Privacy Isolation"
          description="HR & Legal tickets marked as confidential are automatically hidden at database query level from unauthorized roles, guaranteeing strict privacy for payroll disputes and ethics inquiries."
          type="info"
          showIcon
          icon={<LockOutlined />}
          style={{ marginBottom: 24 }}
        />

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={serviceDeskKeys.map(key => ({
              key,
              deskName: `${key} Service Desk`,
              serviceDeskKey: key,
              isVisible: getRuleValue(selectedRole, key, 'isVisible'),
              canCreateTickets: getRuleValue(selectedRole, key, 'canCreateTickets'),
              isConfidential: getRuleValue(selectedRole, key, 'isConfidential')
            }))}
            pagination={false}
            columns={[
              {
                title: 'Department Service Desk',
                dataIndex: 'deskName',
                render: (text: string, record: any) => (
                  <Space>
                    <strong>{text}</strong>
                    {record.isConfidential && (
                      <Tag color="purple" icon={<LockOutlined />}>Confidential Desk</Tag>
                    )}
                  </Space>
                )
              },
              {
                title: 'Desk Visible to Role',
                dataIndex: 'isVisible',
                render: (val: boolean, record: any) => (
                  <Switch
                    checked={val}
                    onChange={() => handleToggle(selectedRole, record.serviceDeskKey, 'isVisible', val)}
                    checkedChildren="Visible"
                    unCheckedChildren="Hidden"
                  />
                )
              },
              {
                title: 'Can Submit Tickets',
                dataIndex: 'canCreateTickets',
                render: (val: boolean, record: any) => (
                  <Switch
                    checked={val}
                    onChange={() => handleToggle(selectedRole, record.serviceDeskKey, 'canCreateTickets', val)}
                    checkedChildren="Allowed"
                    unCheckedChildren="Blocked"
                  />
                )
              },
              {
                title: 'Strict Privacy Isolation',
                dataIndex: 'isConfidential',
                render: (val: boolean, record: any) => (
                  <Switch
                    checked={val}
                    onChange={() => handleToggle(selectedRole, record.serviceDeskKey, 'isConfidential', val)}
                    checkedChildren="Strict Privacy"
                    unCheckedChildren="Standard"
                  />
                )
              }
            ]}
          />
        )}
      </Card>
    </div>
  );
};

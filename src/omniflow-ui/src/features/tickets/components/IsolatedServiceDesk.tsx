import React, { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Typography, Space, Tag } from 'antd';
import { AppstoreOutlined } from '@ant-design/icons';
import { EnterpriseDataGrid, type GridFetchParams } from '../../../shared/components/datagrid/EnterpriseDataGrid';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

interface DeskTicket {
  id: string;
  title: string;
  currentState: string;
  priority: string;
}

/**
 * Tickets scoped to a single service desk. The backend exposes
 * /api/tickets/servicedesk/{id}; the list is small per desk, so it's filtered
 * and paged client-side through the enterprise grid.
 */
export const IsolatedServiceDesk: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [rows, setRows] = useState<DeskTicket[]>([]);
  const [deskName, setDeskName] = useState('');

  useEffect(() => {
    if (!id) return;
    (async () => {
      const [tickets, desks] = await Promise.all([
        api.get(`/tickets/servicedesk/${id}`),
        api.get('/servicedesks'),
      ]);
      setRows(tickets.data ?? []);
      const list = desks.data?.items ?? desks.data ?? [];
      setDeskName(list.find((d: { id: string; name: string }) => d.id === id)?.name ?? '');
    })().catch(() => undefined);
  }, [id]);

  const fetchData = useCallback(async ({ page, pageSize, search }: GridFetchParams) => {
    let data = rows;
    if (search) {
      const needle = search.toLowerCase();
      data = data.filter(r => r.title?.toLowerCase().includes(needle));
    }
    const start = (page - 1) * pageSize;
    return { data: data.slice(start, start + pageSize), total: data.length };
  }, [rows]);

  return (
    <div>
      <Space align="center" style={{ marginBottom: 18 }}>
        <AppstoreOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
        <Typography.Title level={3} style={{ margin: 0 }}>
          {deskName || t('sidebar.servicedeskportal', 'Service Desk')}
        </Typography.Title>
      </Space>

      <EnterpriseDataGrid<DeskTicket>
        // Remount when the loaded set changes so the grid re-reads the fetched rows.
        key={`desk-${id}-${rows.length}`}
        title={deskName}
        rowKey="id"
        columns={[
          { title: t('tickets.title', 'Title'), dataIndex: 'title', key: 'title', sorter: true },
          {
            title: t('tickets.status', 'Status'), dataIndex: 'currentState', key: 'currentState',
            render: (v: string) => <Tag>{t(`ticketState.${v}`, v)}</Tag>,
          },
          { title: t('tickets.priority', 'Priority'), dataIndex: 'priority', key: 'priority' },
        ]}
        fetchData={fetchData}
        onRowDoubleClick={(r) => navigate(`/tickets/${r.id}`)}
        exportable
      />
    </div>
  );
};

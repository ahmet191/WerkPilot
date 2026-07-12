import { useEffect, useState, useCallback, useMemo } from 'react';
import { Spin, Modal, message } from 'antd';
import { EyeOutlined, EditOutlined, DeleteOutlined, ThunderboltOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { EnterpriseDataGrid } from '../datagrid/EnterpriseDataGrid';
import type { GridFetchParams, GridToolbarAction, GroupableColumn } from '../datagrid/EnterpriseDataGrid';
import { DynamicFormModal } from './DynamicFormModal';
import { DynamicViewModal, renderTypedValue } from './DynamicViewModal';
import { SchemaService } from '../../../services/schema.service';
import { api } from '../../../core/api';
import type { UiSchema, GridAction } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';

interface DynamicPageProps {
  entityType: string;
}

/** UI entity types whose detail view embeds the approval timeline. */
const APPROVAL_ENTITY_TYPES: Record<string, string> = {
  tickets: 'Ticket',
  leaves: 'LeaveRequest',
};

export function DynamicPage({ entityType }: DynamicPageProps) {
  const { t } = useTranslation();
  const [schema, setSchema] = useState<UiSchema | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [editRecord, setEditRecord] = useState<any>(null);
  const [editEndpoint, setEditEndpoint] = useState<string | null>(null);
  const [viewRecord, setViewRecord] = useState<any>(null);

  useEffect(() => {
    const loadSchema = async () => {
      setLoading(true);
      try {
        const s = await SchemaService.getSchema(entityType);
        setSchema(s);
      } catch (error) {
        console.error('Failed to load schema for', entityType, error);
      } finally {
        setLoading(false);
      }
    };
    loadSchema();
  }, [entityType]);

  /** Server-side fetch: paging, search, sort and per-column filters all travel
   *  as query parameters; the API returns a single page. Legacy endpoints that
   *  still return raw arrays degrade to client-side slicing. */
  const handleFetchData = useCallback(async ({ page, pageSize, search, sortBy, sortDesc, filters }: GridFetchParams) => {
    if (!schema?.grid) return { data: [], total: 0 };

    try {
      const endpoint = schema.grid.endpoint;
      const params = new URLSearchParams();
      params.set('current', String(page));
      params.set('pageSize', String(pageSize));
      if (search) params.set('search', search);
      if (sortBy) {
        params.set('sortBy', sortBy);
        params.set('sortDesc', String(!!sortDesc));
      }
      Object.entries(filters).forEach(([field, value]) => {
        params.set(`filters[${field}]`, value);
      });

      const separator = endpoint.includes('?') ? '&' : '?';
      const response = await api.get(`${endpoint}${separator}${params.toString()}`);

      // PagedResponse shape (server-side grid endpoints)
      if (response.data && response.data.items !== undefined) {
        return { data: response.data.items, total: response.data.totalCount };
      }

      // Fallback for non-paginated legacy endpoints (e.g. the menu tree)
      const all: any[] = response.data;
      const paged = all.slice((page - 1) * pageSize, page * pageSize);
      return { data: paged, total: all.length };
    } catch (err) {
      console.error('Failed to fetch data', err);
      return { data: [], total: 0 };
    }
  }, [schema, refreshKey]); // refreshKey forces a refetch after mutations

  const grid = schema?.grid;

  const openEdit = useCallback((record: any, action?: GridAction) => {
    const endpointTemplate = action?.endpoint
      ?? grid?.rowActions?.find(a => a.actionType === 'edit')?.endpoint;
    if (!endpointTemplate) return;
    setEditRecord(record);
    setEditEndpoint(endpointTemplate.replace('{id}', record.id));
    setIsFormOpen(true);
  }, [grid]);

  const handleDelete = useCallback((records: any[], action: GridAction) => {
    Modal.confirm({
      title: t('delete.confirmTitle', 'Are you sure you want to delete this?'),
      content: records.length > 1
        ? t('delete.confirmMulti', records.length)
        : t('delete.confirmContent', 'This action cannot be undone.'),
      okText: t('delete.ok', 'Yes, delete'),
      okType: 'danger',
      cancelText: t('common.cancel', 'Cancel'),
      onOk: async () => {
        try {
          for (const record of records) {
            await api.delete(action.endpoint.replace('{id}', record.id));
          }
          message.success(t('delete.success', 'Deleted successfully'));
          setRefreshKey(prev => prev + 1);
        } catch (err) {
          console.error('Delete failed', err);
        }
      },
    });
  }, [t]);

  const handleCustomAction = useCallback(async (record: any, action: GridAction) => {
    try {
      const url = action.endpoint.replace('{id}', record.id);
      if (action.method.toUpperCase() === 'PUT') await api.put(url);
      else if (action.method.toUpperCase() === 'POST') await api.post(url);
      message.success(t('common.done', 'Done'));
      setRefreshKey(prev => prev + 1);
    } catch (err) {
      console.error('Action failed', err);
    }
  }, [t]);

  /** Row actions from the schema become toolbar buttons acting on the current
   *  selection — mutation buttons never render inside data rows. */
  const toolbarActions: GridToolbarAction[] = useMemo(() => {
    if (!grid) return [];

    const actions: GridToolbarAction[] = [
      {
        key: 'view',
        label: t('grid.view', 'View'),
        icon: <EyeOutlined />,
        requiresSelection: 'single',
        onClick: rows => setViewRecord(rows[0]),
      },
    ];

    for (const action of grid.rowActions ?? []) {
      if (action.actionType === 'edit') {
        actions.push({
          key: 'edit',
          label: t(action.labelKey, 'Edit'),
          icon: <EditOutlined />,
          requiresSelection: 'single',
          onClick: rows => openEdit(rows[0], action),
        });
      } else if (action.actionType === 'delete' || action.method.toUpperCase() === 'DELETE') {
        actions.push({
          key: 'delete',
          label: t(action.labelKey, 'Delete'),
          icon: <DeleteOutlined />,
          danger: true,
          requiresSelection: 'multi',
          onClick: rows => handleDelete(rows, action),
        });
      } else {
        actions.push({
          key: action.labelKey,
          label: t(action.labelKey),
          icon: <ThunderboltOutlined />,
          requiresSelection: 'single',
          onClick: rows => handleCustomAction(rows[0], action),
        });
      }
    }

    return actions;
  }, [grid, t, openEdit, handleDelete, handleCustomAction]);

  if (loading || !schema || !grid) {
    return <div style={{ padding: 50, textAlign: 'center' }}><Spin size="large" /></div>;
  }

  // Map backend grid schema to AntD columns (rendering only — actions live in the toolbar).
  const columns: ColumnsType<any> = grid.columns.map(col => ({
    title: t(col.titleKey),
    dataIndex: col.dataIndex,
    key: col.dataIndex,
    sorter: col.sortable ? true : undefined,
    width: col.width,
    ellipsis: col.ellipsis,
    render: (text: any) => renderTypedValue(text, col.type),
  }));

  const filterableColumns = grid.columns.filter(c => c.filterable).map(c => c.dataIndex);

  const groupableColumns: GroupableColumn[] = grid.columns
    .filter(c => c.type === 'tag' || c.type === 'string' || !c.type)
    .map(c => ({ key: c.dataIndex, title: t(c.titleKey) }));

  return (
    <>
      <EnterpriseDataGrid
        title={t(grid.titleKey)}
        columns={columns}
        filterableColumns={filterableColumns}
        groupableColumns={groupableColumns}
        fetchData={handleFetchData}
        toolbarActions={toolbarActions}
        exportable={grid.actions.includes('export')}
        onRowDoubleClick={record => setViewRecord(record)}
        onAdd={grid.actions.includes('add') ? () => {
          setEditRecord(null);
          setEditEndpoint(null);
          setIsFormOpen(true);
        } : undefined}
      />

      <DynamicViewModal
        open={viewRecord !== null}
        record={viewRecord}
        columns={grid.columns}
        title={t(grid.titleKey)}
        approvalEntityType={APPROVAL_ENTITY_TYPES[entityType]}
        onCancel={() => setViewRecord(null)}
        onEdit={grid.rowActions?.some(a => a.actionType === 'edit') ? (record) => {
          setViewRecord(null);
          openEdit(record);
        } : undefined}
      />

      {schema.form && (
        <DynamicFormModal
          open={isFormOpen}
          schema={schema.form}
          editRecord={editRecord}
          editEndpoint={editEndpoint}
          onCancel={() => {
            setIsFormOpen(false);
            setEditRecord(null);
            setEditEndpoint(null);
          }}
          onSuccess={() => {
            setIsFormOpen(false);
            setEditRecord(null);
            setEditEndpoint(null);
            setRefreshKey(prev => prev + 1);
          }}
        />
      )}
    </>
  );
}

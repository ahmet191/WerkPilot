import React, { useEffect, useState, useCallback } from 'react';
import { Tag, Spin } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { EnterpriseDataGrid } from '../datagrid/EnterpriseDataGrid';
import { DynamicFormModal } from './DynamicFormModal';
import { SchemaService } from '../../../services/schema.service';
import { api } from '../../../core/api';
import type { UiSchema } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';

interface DynamicPageProps {
  entityType: string;
}

export function DynamicPage({ entityType }: DynamicPageProps) {
  const { t } = useTranslation();
  const [schema, setSchema] = useState<UiSchema | null>(null);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

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

  const handleFetchData = useCallback(async (page: number, pageSize: number, search: string, sortBy?: string, sortDesc?: boolean) => {
    if (!schema) return { data: [], total: 0 };
    
    try {
      const endpoint = schema.grid.endpoint;
      const separator = endpoint.includes('?') ? '&' : '?';
      let url = `${endpoint}${separator}current=${page}&pageSize=${pageSize}`;
      
      if (search) url += `&search=${encodeURIComponent(search)}`;
      if (sortBy) url += `&sortBy=${encodeURIComponent(sortBy)}&sortDesc=${sortDesc}`;

      const response = await api.get(url);
      
      // Check if response is PagedResponse (has items and totalCount)
      if (response.data && response.data.items !== undefined) {
          return { data: response.data.items, total: response.data.totalCount };
      }
      
      // Fallback for non-paginated legacy endpoints
      const all = response.data;
      const paged = all.slice((page - 1) * pageSize, page * pageSize);
      return { data: paged, total: all.length };
    } catch (err) {
      console.error('Failed to fetch data', err);
      return { data: [], total: 0 };
    }
  }, [schema, refreshKey]); // Include refreshKey so grid refetches on save

  if (loading || !schema) {
    return <div style={{ padding: 50, textAlign: 'center' }}><Spin size="large" /></div>;
  }

  // Map Backend JSON Schema to Ant Design Columns
  const columns: ColumnsType<any> = schema.grid.columns.map(col => {
    return {
      title: t(col.titleKey),
      dataIndex: col.dataIndex,
      key: col.dataIndex,
      sorter: col.sortable ? true : undefined,
      width: col.width,
      ellipsis: col.ellipsis,
      render: (text: any) => {
        if (col.type === 'tag') {
          return <Tag color={text === 'Assigned' ? 'blue' : 'green'}>{text}</Tag>;
        }
        return text;
      }
    };
  });

  const handleActionClick = async (action: any, record: any) => {
    const url = action.endpoint.replace('{id}', record.id);
    try {
      if (action.method.toUpperCase() === 'PUT') {
        await api.put(url);
      } else if (action.method.toUpperCase() === 'POST') {
        await api.post(url);
      } else if (action.method.toUpperCase() === 'DELETE') {
        await api.delete(url);
      }
      setRefreshKey(prev => prev + 1);
    } catch (err) {
      console.error('Action failed', err);
    }
  };

  if (schema.grid.rowActions && schema.grid.rowActions.length > 0) {
    columns.push({
      title: t('grid.actions') || 'Actions',
      key: 'actions',
      width: 150,
      render: (_, record) => (
        <span style={{ display: 'flex', gap: '8px' }}>
          {schema.grid.rowActions?.map((action, idx) => (
            <a
              key={idx}
              style={{ color: action.color === 'danger' ? 'red' : 'var(--primary-color)', cursor: 'pointer' }}
              onClick={() => handleActionClick(action, record)}
            >
              {t(action.labelKey)}
            </a>
          ))}
        </span>
      ),
    });
  }

  return (
    <>
      <EnterpriseDataGrid
        title={t(schema.grid.titleKey)}
        columns={columns}
        fetchData={handleFetchData}
        onAdd={schema.grid.actions.includes('add') ? () => setIsModalOpen(true) : undefined}
      />
      
      {schema.form && (
        <DynamicFormModal
          open={isModalOpen}
          schema={schema.form}
          onCancel={() => setIsModalOpen(false)}
          onSuccess={() => {
            setIsModalOpen(false);
            setRefreshKey(prev => prev + 1); // Trigger grid refresh
          }}
        />
      )}
    </>
  );
}

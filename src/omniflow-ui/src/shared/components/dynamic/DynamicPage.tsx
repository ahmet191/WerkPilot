import React, { useEffect, useState, useCallback } from 'react';
import { Tag, Spin } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { EnterpriseDataGrid } from '../datagrid/EnterpriseDataGrid';
import { DynamicFormModal } from './DynamicFormModal';
import { SchemaService } from '../../../services/schema.service';
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

  const handleFetchData = useCallback(async (page: number, pageSize: number, search: string) => {
    if (!schema) return { data: [], total: 0 };
    
    // In a real implementation, you'd pass ?page=1&limit=20&search=xyz to the backend.
    // For our demo, we fetch all and slice it on the client side since the current /api/departments endpoint doesn't support pagination yet.
    const all = await SchemaService.fetchGridData(schema.grid.endpoint);
    
    let filtered = all;
    if (search) {
        filtered = all.filter(item => JSON.stringify(item).toLowerCase().includes(search.toLowerCase()));
    }
    
    const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
    return {
      data: paged,
      total: filtered.length,
    };
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
      sorter: col.sortable ? (a, b) => {
          const valA = a[col.dataIndex] || '';
          const valB = b[col.dataIndex] || '';
          return valA.toString().localeCompare(valB.toString());
      } : undefined,
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

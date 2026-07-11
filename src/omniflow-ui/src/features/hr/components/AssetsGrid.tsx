import React, { useCallback } from 'react';
import { EnterpriseDataGrid } from '../../../shared/components/datagrid/EnterpriseDataGrid';
import { HrService } from '../../../services/hr.service';
import type { AssetDto } from '../models/hr.models';
import { Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useTranslation } from '../../../core/LanguageContext';

export function AssetsGrid() {
  const { t } = useTranslation();

  const handleFetchData = useCallback(async (page: number, pageSize: number, search: string) => {
    const all = await HrService.getAssets();
    const filtered = all.filter(d => 
      d.assetTag.toLowerCase().includes(search.toLowerCase()) || 
      d.model.toLowerCase().includes(search.toLowerCase())
    );
    const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
    return {
      data: paged,
      total: filtered.length,
    };
  }, []);

  const columns: ColumnsType<AssetDto> = [
    { title: t('assets.tag'), dataIndex: 'assetTag', key: 'assetTag', width: 120 },
    { 
      title: t('assets.category'), 
      dataIndex: 'category', 
      key: 'category', 
      width: 150,
      filters: [{ text: 'Laptop', value: 'Laptop' }, { text: 'Mobile', value: 'Mobile' }],
      onFilter: (value, record) => record.category === value,
    },
    { title: t('assets.model'), dataIndex: 'model', key: 'model' },
    { title: t('assets.serial'), dataIndex: 'serialNumber', key: 'serialNumber' },
    { 
      title: t('assets.status'), 
      dataIndex: 'status', 
      key: 'status', 
      width: 100,
      render: (status: string) => (
        <Tag color={status === 'Assigned' ? 'blue' : 'green'}>{status}</Tag>
      ),
    },
    { title: t('assets.assigned'), dataIndex: 'assignedToUserId', key: 'assignedToUserId', ellipsis: true },
  ];

  return (
    <EnterpriseDataGrid<AssetDto>
      title={t('assets.title')}
      columns={columns}
      fetchData={handleFetchData}
      onAdd={() => console.log('Add asset')}
      onExport={(data) => console.log('Export assets', data)}
    />
  );
}

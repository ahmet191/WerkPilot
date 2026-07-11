import React, { useCallback } from 'react';
import { EnterpriseDataGrid } from '../../../shared/components/datagrid/EnterpriseDataGrid';
import { HrService } from '../../../services/hr.service';
import type { DepartmentDto } from '../models/hr.models';
import type { ColumnsType } from 'antd/es/table';
import { useTranslation } from '../../../core/LanguageContext';

export function DepartmentsGrid() {
  const { t } = useTranslation();

  const handleFetchData = useCallback(async (page: number, pageSize: number, search: string) => {
    const all = await HrService.getDepartments();
    // Simulate server side searching/filtering and paging for this demo
    const filtered = all.filter(d => d.name.toLowerCase().includes(search.toLowerCase()));
    const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
    return {
      data: paged,
      total: filtered.length,
    };
  }, []);

  const columns: ColumnsType<DepartmentDto> = [
    { title: t('departments.id'), dataIndex: 'id', key: 'id', width: 250, ellipsis: true },
    { title: t('departments.name'), dataIndex: 'name', key: 'name', sorter: (a, b) => a.name.localeCompare(b.name) },
    { title: t('departments.manager'), dataIndex: 'managerId', key: 'managerId' },
  ];

  return (
    <EnterpriseDataGrid<DepartmentDto>
      title={t('departments.title')}
      columns={columns}
      fetchData={handleFetchData}
      onAdd={() => console.log('Add department')}
      onExport={(data) => console.log('Export departments', data)}
    />
  );
}

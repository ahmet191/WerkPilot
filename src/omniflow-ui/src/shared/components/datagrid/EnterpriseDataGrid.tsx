import React, { useState, useEffect, useCallback } from 'react';
import { Table, Button, Input, Space, Tooltip } from 'antd';
import type { ColumnsType, TableProps } from 'antd/es/table';
import { ReloadOutlined, PlusOutlined, DownloadOutlined, SearchOutlined } from '@ant-design/icons';
import { useTranslation } from '../../../core/LanguageContext';

interface EnterpriseDataGridProps<T> extends Omit<TableProps<T>, 'dataSource' | 'columns' | 'loading'> {
  title: string;
  columns: ColumnsType<T>;
  fetchData: (page: number, pageSize: number, search: string) => Promise<{ data: T[]; total: number }>;
  onAdd?: () => void;
  onExport?: (data: T[]) => void;
  actions?: React.ReactNode;
}

export function EnterpriseDataGrid<T extends object>({
  title,
  columns,
  fetchData,
  onAdd,
  onExport,
  actions,
  ...tableProps
}: EnterpriseDataGridProps<T>) {
  const { t } = useTranslation();
  const [data, setData] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState('');

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const result = await fetchData(page, pageSize, search);
      setData(result.data);
      setTotal(result.total);
    } catch (error) {
      console.error('DataGrid fetch failed:', error);
    } finally {
      setLoading(false);
    }
  }, [fetchData, page, pageSize, search]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Debounced search logic could go here, or simple onSearch on change/submit
  const handleSearch = (val: string) => {
    setSearch(val);
    setPage(1); // Reset page to 1 on search
  };

  return (
    <div className="enterprise-grid">
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '8px 12px',
          borderBottom: '1px solid var(--border-color)',
          background: 'var(--bg-dark)',
        }}
      >
        <h3 style={{ margin: 0, fontSize: '14px', fontWeight: 600 }}>{title}</h3>
        <Space size="small">
          <Input
            size="small"
            placeholder={t('grid.search')}
            prefix={<SearchOutlined />}
            onChange={(e) => handleSearch(e.target.value)}
            style={{ width: 180 }}
            allowClear
          />
          <Tooltip title="Refresh">
            <Button size="small" icon={<ReloadOutlined />} onClick={loadData} loading={loading} />
          </Tooltip>
          {onExport && (
            <Tooltip title="Export to CSV">
              <Button size="small" icon={<DownloadOutlined />} onClick={() => onExport(data)} />
            </Tooltip>
          )}
          {actions}
          {onAdd && (
            <Button size="small" type="primary" icon={<PlusOutlined />} onClick={onAdd}>
              {t('grid.new')}
            </Button>
          )}
        </Space>
      </div>
      <Table<T>
        size="small"
        scroll={{ y: 'calc(100vh - 200px)' }}
        dataSource={data}
        columns={columns}
        loading={loading}
        pagination={{
          size: 'small',
          current: page,
          pageSize: pageSize,
          total: total,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50', '100'],
          showTotal: (totalCount) => t('grid.total', totalCount),
          onChange: (p, ps) => {
            setPage(p);
            setPageSize(ps);
          },
        }}
        {...tableProps}
      />
    </div>
  );
}

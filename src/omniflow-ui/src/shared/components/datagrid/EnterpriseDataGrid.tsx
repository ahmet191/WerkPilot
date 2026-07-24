import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { Table, Button, Input, Space, Tooltip, Select, Tag, Divider, Empty } from 'antd';
import type { ColumnsType, TableProps } from 'antd/es/table';
import {
  ReloadOutlined,
  PlusOutlined,
  DownloadOutlined,
  SearchOutlined,
  FilterFilled,
  GroupOutlined,
} from '@ant-design/icons';
import { useTranslation } from '../../../core/LanguageContext';

/** Parameters sent to the server on every grid fetch. */
export interface GridFetchParams {
  page: number;
  pageSize: number;
  search: string;
  sortBy?: string;
  sortDesc?: boolean;
  /** Per-column filter values keyed by dataIndex. */
  filters: Record<string, string>;
}

/**
 * A toolbar button rendered ABOVE the table. Buttons never live inside data
 * rows; they act on the current row selection instead.
 */
export interface GridToolbarAction {
  key: string;
  label: string;
  icon?: React.ReactNode;
  type?: 'default' | 'primary';
  danger?: boolean;
  /** 'none' = always enabled, 'single' = exactly one row, 'multi' = one or more. */
  requiresSelection?: 'none' | 'single' | 'multi';
  onClick: (selectedRows: any[]) => void;
}

export interface GroupableColumn {
  key: string;
  title: string;
}

interface EnterpriseDataGridProps<T> extends Omit<TableProps<T>, 'dataSource' | 'columns' | 'loading' | 'title' | 'onChange'> {
  title: string;
  columns: ColumnsType<T>;
  /** dataIndexes that get a server-side filter dropdown. */
  filterableColumns?: string[];
  /** Columns offered in the "Group by" selector (grouping is applied to the current page). */
  groupableColumns?: GroupableColumn[];
  fetchData: (params: GridFetchParams) => Promise<{ data: T[]; total: number }>;
  toolbarActions?: GridToolbarAction[];
  onAdd?: () => void;
  onRowDoubleClick?: (record: T) => void;
  /** Show the CSV export button (exports the current page). */
  exportable?: boolean;
}

const GROUP_ROW_KEY_PREFIX = '__group__';

/** Search-input dropdown rendered in a column header for server-side filtering. */
function ColumnFilterDropdown({
  defaultValue,
  applyLabel,
  resetLabel,
  placeholder,
  onApply,
  confirm,
}: {
  defaultValue: string;
  applyLabel: string;
  resetLabel: string;
  placeholder: string;
  onApply: (value: string) => void;
  confirm: (opts?: { closeDropdown: boolean }) => void;
}) {
  const [value, setValue] = useState(defaultValue);

  const commit = (v: string) => {
    onApply(v.trim());
    confirm({ closeDropdown: true });
  };

  return (
    <div style={{ padding: 8 }} onKeyDown={e => e.stopPropagation()}>
      <Input
        size="small"
        autoFocus
        placeholder={placeholder}
        value={value}
        onChange={e => setValue(e.target.value)}
        onPressEnter={() => commit(value)}
        style={{ width: 190, display: 'block', marginBottom: 8 }}
      />
      <Space>
        <Button size="small" type="primary" onClick={() => commit(value)}>
          {applyLabel}
        </Button>
        <Button size="small" onClick={() => { setValue(''); commit(''); }}>
          {resetLabel}
        </Button>
      </Space>
    </div>
  );
}

/** Server-side driven data grid: selection, toolbar actions, per-column
 *  filter/sort, free-text search, page grouping and CSV export. */
export function EnterpriseDataGrid<T extends { id?: string | number }>({
  title,
  columns,
  filterableColumns = [],
  groupableColumns = [],
  fetchData,
  toolbarActions = [],
  onAdd,
  onRowDoubleClick,
  exportable = false,
  ...tableProps
}: EnterpriseDataGridProps<T>) {
  const { t } = useTranslation();
  const [data, setData] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState<string | undefined>();
  const [sortDesc, setSortDesc] = useState<boolean | undefined>();
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [selectedRows, setSelectedRows] = useState<T[]>([]);
  const [groupBy, setGroupBy] = useState<string | undefined>();
  const [expandedGroupKeys, setExpandedGroupKeys] = useState<readonly React.Key[]>([]);
  const searchDebounce = useRef<ReturnType<typeof setTimeout> | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const result = await fetchData({ page, pageSize, search, sortBy, sortDesc, filters });
      setData(result.data);
      setTotal(result.total);
    } catch (error) {
      console.error('DataGrid fetch failed:', error);
    } finally {
      setLoading(false);
    }
  }, [fetchData, page, pageSize, search, sortBy, sortDesc, filters]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // A fresh page invalidates the previous selection.
  useEffect(() => {
    setSelectedRowKeys([]);
    setSelectedRows([]);
  }, [data]);

  const handleSearch = (val: string) => {
    if (searchDebounce.current) clearTimeout(searchDebounce.current);
    searchDebounce.current = setTimeout(() => {
      setSearch(val);
      setPage(1);
    }, 350);
  };

  const applyColumnFilter = (dataIndex: string, value: string) => {
    setFilters(prev => {
      const next = { ...prev };
      if (value) next[dataIndex] = value;
      else delete next[dataIndex];
      return next;
    });
    setPage(1);
  };

  /** Injects a server-side filter dropdown into filterable columns. */
  const decoratedColumns: ColumnsType<T> = useMemo(() => {
    const cols = columns.map(col => {
      const key = String((col as any).dataIndex ?? col.key ?? '');
      if (!filterableColumns.includes(key)) return col;

      return {
        ...col,
        filterDropdown: ({ confirm }: any) => (
          <ColumnFilterDropdown
            defaultValue={filters[key] ?? ''}
            placeholder={`${t('grid.filter', 'Filter')}...`}
            applyLabel={t('grid.apply', 'Apply')}
            resetLabel={t('grid.reset', 'Reset')}
            onApply={value => applyColumnFilter(key, value)}
            confirm={confirm}
          />
        ),
        filterIcon: () => (
          <FilterFilled style={{ color: filters[key] ? 'var(--accent-color, #2f81f7)' : undefined }} />
        ),
      };
    });

    // When grouping, the group header row spans the full width of the table.
    if (groupBy && cols.length > 0) {
      return cols.map((col, idx) => ({
        ...col,
        onCell: (record: any) => {
          if (!record[GROUP_ROW_KEY_PREFIX]) return {};
          return idx === 0 ? { colSpan: cols.length } : { colSpan: 0 };
        },
        render: (value: any, record: any, index: number) => {
          if (record[GROUP_ROW_KEY_PREFIX] && idx === 0) {
            return (
              <strong>
                {record.__groupLabel} <Tag style={{ marginLeft: 8 }}>{record.children?.length ?? 0}</Tag>
              </strong>
            );
          }
          const original = (col as any).render;
          return original ? original(value, record, index) : value;
        },
      }));
    }

    return cols;
  }, [columns, filterableColumns, filters, groupBy, t]);

  /** Client-side grouping of the CURRENT page into an expandable tree. */
  const groupedData: any[] = useMemo(() => {
    if (!groupBy) return data;

    const groups = new Map<string, T[]>();
    data.forEach(row => {
      const raw = (row as any)[groupBy];
      const key = raw === null || raw === undefined || raw === '' ? '—' : String(raw);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(row);
    });

    return [...groups.entries()].map(([label, rows]) => ({
      id: `${GROUP_ROW_KEY_PREFIX}${label}`,
      [GROUP_ROW_KEY_PREFIX]: true,
      __groupLabel: label,
      children: rows,
    }));
  }, [data, groupBy]);

  // New grouping or fresh data: expand every group by default.
  useEffect(() => {
    if (groupBy) {
      setExpandedGroupKeys(groupedData.map((g: any) => g.id));
    }
  }, [groupBy, groupedData]);

  const exportCsv = () => {
    const headers = columns.map(c => String(c.title ?? (c as any).dataIndex ?? ''));
    const keys = columns.map(c => String((c as any).dataIndex ?? c.key ?? ''));
    const escape = (v: any) => {
      const s = v === null || v === undefined ? '' : typeof v === 'object' ? JSON.stringify(v) : String(v);
      return `"${s.replace(/"/g, '""')}"`;
    };
    const lines = [
      headers.map(escape).join(','),
      ...data.map(row => keys.map(k => escape((row as any)[k])).join(',')),
    ];
    const blob = new Blob([`﻿${lines.join('\n')}`], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${title.replace(/\s+/g, '_').toLowerCase()}_export.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const isActionEnabled = (action: GridToolbarAction) => {
    switch (action.requiresSelection ?? 'none') {
      case 'single': return selectedRows.length === 1;
      case 'multi': return selectedRows.length >= 1;
      default: return true;
    }
  };

  const activeFilterCount = Object.keys(filters).length;

  return (
    <div className="enterprise-grid">
      {/* ── Toolbar: title, selection info, data actions, search ──
           Every control here is functional (server-side sort/filter/search,
           grouping, selection-aware actions) — styling must never remove one. */}
      <div className="grid-toolbar">
        <Space size="small" wrap>
          <h3 className="grid-title">{title}</h3>
          {selectedRowKeys.length > 0 && (
            <Tag color="blue" style={{ borderRadius: 999 }}>
              {t('grid.selected', selectedRowKeys.length)}
            </Tag>
          )}
          {/* One chip per active column filter — visible, individually removable. */}
          {Object.entries(filters).map(([field, value]) => (
            <Tag
              key={field}
              closable
              onClose={() => applyColumnFilter(field, '')}
              icon={<FilterFilled style={{ fontSize: 10 }} />}
              style={{ borderRadius: 999 }}
            >
              {field}: {value}
            </Tag>
          ))}
          {activeFilterCount > 1 && (
            <Button type="link" size="small" style={{ padding: 0, fontSize: 12 }}
              onClick={() => { setFilters({}); setPage(1); }}>
              {t('grid.clearFilters', 'Clear all')}
            </Button>
          )}
        </Space>

        <Space size="small" wrap>
          {onAdd && (
            <Button size="small" type="primary" icon={<PlusOutlined />} onClick={onAdd}>
              {t('grid.new')}
            </Button>
          )}
          {toolbarActions.map(action => (
            <Button
              key={action.key}
              size="small"
              type={action.type ?? 'default'}
              danger={action.danger}
              icon={action.icon}
              disabled={!isActionEnabled(action)}
              onClick={() => action.onClick(selectedRows)}
            >
              {action.label}
            </Button>
          ))}

          <Divider type="vertical" style={{ margin: '0 2px' }} />

          {groupableColumns.length > 0 && (
            <Select
              size="small"
              allowClear
              placeholder={<span><GroupOutlined /> {t('grid.groupBy', 'Group by')}</span>}
              style={{ minWidth: 150 }}
              value={groupBy}
              onChange={value => setGroupBy(value)}
              options={groupableColumns.map(g => ({ label: g.title, value: g.key }))}
            />
          )}
          <Input
            size="small"
            placeholder={t('grid.search')}
            prefix={<SearchOutlined />}
            onChange={(e) => handleSearch(e.target.value)}
            style={{ width: 180 }}
            allowClear
          />
          <Tooltip title={t('grid.refresh', 'Refresh')}>
            <Button size="small" icon={<ReloadOutlined />} onClick={loadData} loading={loading} />
          </Tooltip>
          {exportable && (
            <Tooltip title={t('grid.export', 'Export CSV')}>
              <Button size="small" icon={<DownloadOutlined />} onClick={exportCsv} />
            </Tooltip>
          )}
        </Space>
      </div>

      <Table<T>
        size="small"
        rowKey="id"
        scroll={{ x: 'max-content', y: 'calc(100vh - 240px)' }}
        dataSource={groupedData}
        columns={decoratedColumns}
        loading={loading}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              styles={{ image: { height: 52 } }}
              description={
                <span style={{ color: 'var(--text-tertiary)' }}>
                  {activeFilterCount > 0 || search
                    ? t('grid.noMatches', 'No records match your filters')
                    : t('grid.noData', 'No data')}
                </span>
              }
            />
          ),
        }}
        expandable={groupBy ? {
          expandedRowKeys: expandedGroupKeys,
          onExpandedRowsChange: keys => setExpandedGroupKeys(keys),
        } : undefined}
        rowSelection={{
          selectedRowKeys,
          checkStrictly: false,
          onChange: (keys, rows) => {
            const realRows = (rows as any[]).filter(r => r && !r[GROUP_ROW_KEY_PREFIX]);
            setSelectedRowKeys(keys.filter(k => !String(k).startsWith(GROUP_ROW_KEY_PREFIX)));
            setSelectedRows(realRows as T[]);
          },
          getCheckboxProps: (record: any) => ({
            disabled: !!record[GROUP_ROW_KEY_PREFIX],
          }),
        }}
        onRow={(record: any) => ({
          onClick: () => {
            if (record[GROUP_ROW_KEY_PREFIX]) return;
            // Single click selects exactly this row; checkboxes handle multi-select.
            setSelectedRowKeys([record.id]);
            setSelectedRows([record]);
          },
          onDoubleClick: () => {
            if (record[GROUP_ROW_KEY_PREFIX]) return;
            onRowDoubleClick?.(record);
          },
          style: { cursor: 'pointer' },
        })}
        pagination={{
          size: 'small',
          current: page,
          pageSize: pageSize,
          total: total,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50', '100'],
          showTotal: (totalCount) => t('grid.total', totalCount),
        }}
        onChange={(pagination, _antFilters, sorter: any) => {
          if (pagination.current) setPage(pagination.current);
          if (pagination.pageSize) setPageSize(pagination.pageSize);

          const single = Array.isArray(sorter) ? sorter[0] : sorter;
          if (single && single.order && (single.columnKey || single.field)) {
            setSortBy(String(single.columnKey ?? single.field));
            setSortDesc(single.order === 'descend');
          } else {
            setSortBy(undefined);
            setSortDesc(undefined);
          }
        }}
        {...tableProps}
      />
    </div>
  );
}

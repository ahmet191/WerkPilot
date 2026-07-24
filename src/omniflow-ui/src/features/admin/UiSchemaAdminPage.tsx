import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert, Button, Card, Empty, InputNumber, Select, Space, Switch, Table, Tag, Tooltip, Typography, message,
} from 'antd';
import {
  LayoutOutlined, ArrowUpOutlined, ArrowDownOutlined, SaveOutlined, UndoOutlined,
} from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text } = Typography;

interface UiSchemaColumn {
  dataIndex: string;
  titleKey: string;
  type: string;
  sortable: boolean;
  filterable: boolean;
  ellipsis: boolean;
  width?: number | null;
}

interface UiSchema {
  grid: {
    titleKey: string;
    endpoint: string;
    columns: UiSchemaColumn[];
    actions: string[];
    rowActions: unknown[];
  };
  form: unknown;
  widgets: unknown[];
}

interface CustomizableEntity {
  entityType: string;
  isCustomized: boolean;
}

/** A default column paired with whether the tenant currently shows it. */
interface EditorColumn extends UiSchemaColumn {
  visible: boolean;
}

/**
 * Lets a tenant reshape any grid — which columns show, their order, and whether
 * each is sortable or filterable — without a deploy. The platform's shipped
 * layout stays the default; only what the tenant changes is stored. Endpoints
 * and row actions are always taken from the shipped schema, so a layout edit can
 * never repoint a call or unlock an action.
 */
export const UiSchemaAdminPage: React.FC = () => {
  const { t } = useTranslation();
  const [entities, setEntities] = useState<CustomizableEntity[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [defaultSchema, setDefaultSchema] = useState<UiSchema | null>(null);
  const [columns, setColumns] = useState<EditorColumn[]>([]);
  const [isCustomized, setIsCustomized] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [dirty, setDirty] = useState(false);

  const loadEntities = useCallback(async () => {
    const res = await api.get<CustomizableEntity[]>('/uischema/admin/customizable');
    setEntities(res.data ?? []);
    if (!selected && res.data?.length) setSelected(res.data[0].entityType);
  }, [selected]);

  useEffect(() => { loadEntities().catch(() => undefined); }, [loadEntities]);

  const buildEditorColumns = useCallback((def: UiSchema, override: UiSchema | null): EditorColumn[] => {
    const defCols = def.grid?.columns ?? [];
    if (!override) return defCols.map(c => ({ ...c, visible: true }));

    // The override's columns are the visible ones, in the tenant's chosen order;
    // any default column the tenant dropped follows, hidden, so it can be
    // re-added. Match on dataIndex — a column dropped from the shipped schema in
    // a later release simply falls out.
    const ovrCols = override.grid?.columns ?? [];
    const byIndex = new Map(defCols.map(c => [c.dataIndex, c]));
    const seen = new Set<string>();
    const visible: EditorColumn[] = [];
    for (const c of ovrCols) {
      if (!byIndex.has(c.dataIndex) || seen.has(c.dataIndex)) continue;
      seen.add(c.dataIndex);
      visible.push({ ...c, visible: true });
    }
    const hidden: EditorColumn[] = defCols
      .filter(c => !seen.has(c.dataIndex))
      .map(c => ({ ...c, visible: false }));
    return [...visible, ...hidden];
  }, []);

  const loadSchema = useCallback(async (entityType: string) => {
    setLoading(true);
    try {
      const res = await api.get(`/uischema/admin/${entityType}`);
      const def: UiSchema = res.data.default;
      const override: UiSchema | null = res.data.override ?? null;
      setDefaultSchema(def);
      setIsCustomized(Boolean(res.data.isCustomized));
      setColumns(buildEditorColumns(def, override));
      setDirty(false);
    } finally {
      setLoading(false);
    }
  }, [buildEditorColumns]);

  useEffect(() => { if (selected) loadSchema(selected).catch(() => undefined); }, [selected, loadSchema]);

  const move = (index: number, delta: number) => {
    setColumns(prev => {
      const next = [...prev];
      const target = index + delta;
      if (target < 0 || target >= next.length) return prev;
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
    setDirty(true);
  };

  const patch = (dataIndex: string, change: Partial<EditorColumn>) => {
    setColumns(prev => prev.map(c => (c.dataIndex === dataIndex ? { ...c, ...change } : c)));
    setDirty(true);
  };

  const save = async () => {
    if (!selected || !defaultSchema) return;
    const visible = columns.filter(c => c.visible);
    if (visible.length === 0) {
      message.warning(t('layouts.needColumn', 'Keep at least one column visible'));
      return;
    }
    setSaving(true);
    try {
      // Send the default with only the grid's title and columns reshaped. The
      // server re-stamps endpoint, actions, row actions, form and widgets, so
      // those values here are informational only.
      const schema: UiSchema = {
        ...defaultSchema,
        grid: {
          ...defaultSchema.grid,
          columns: visible.map(({ visible: _v, ...col }) => col),
        },
      };
      await api.put(`/uischema/admin/${selected}`, { schema });
      message.success(t('layouts.saved', 'Layout saved'));
      setDirty(false);
      setIsCustomized(true);
      loadEntities().catch(() => undefined);
    } finally {
      setSaving(false);
    }
  };

  const revert = async () => {
    if (!selected) return;
    await api.delete(`/uischema/admin/${selected}`);
    message.success(t('layouts.reverted', 'Reverted to the default layout'));
    await loadSchema(selected);
    loadEntities().catch(() => undefined);
  };

  const entityOptions = useMemo(() => entities.map(e => ({
    value: e.entityType,
    label: (
      <Space>
        <span style={{ textTransform: 'capitalize' }}>{t(`entity.${e.entityType}`, e.entityType)}</span>
        {e.isCustomized && <Tag color="blue">{t('layouts.custom', 'custom')}</Tag>}
      </Space>
    ),
  })), [entities, t]);

  const editorColumns = [
    {
      title: t('layouts.order', 'Order'),
      key: 'order',
      width: 90,
      render: (_: unknown, _row: EditorColumn, index: number) => (
        <Space size={2}>
          <Button size="small" type="text" icon={<ArrowUpOutlined />} disabled={index === 0} onClick={() => move(index, -1)} />
          <Button size="small" type="text" icon={<ArrowDownOutlined />} disabled={index === columns.length - 1} onClick={() => move(index, 1)} />
        </Space>
      ),
    },
    {
      title: t('layouts.visible', 'Shown'),
      key: 'visible',
      width: 80,
      render: (_: unknown, row: EditorColumn) => (
        <Switch size="small" checked={row.visible} onChange={v => patch(row.dataIndex, { visible: v })} />
      ),
    },
    {
      title: t('layouts.column', 'Column'),
      key: 'label',
      render: (_: unknown, row: EditorColumn) => (
        <Space direction="vertical" size={0}>
          <span>{t(row.titleKey, row.titleKey)}</span>
          <Text type="secondary" style={{ fontSize: 12 }}>{row.dataIndex}</Text>
        </Space>
      ),
    },
    {
      title: t('layouts.sortable', 'Sortable'),
      key: 'sortable',
      width: 110,
      render: (_: unknown, row: EditorColumn) => (
        <Switch size="small" checked={row.sortable} disabled={!row.visible} onChange={v => patch(row.dataIndex, { sortable: v })} />
      ),
    },
    {
      title: t('layouts.filterable', 'Filterable'),
      key: 'filterable',
      width: 110,
      render: (_: unknown, row: EditorColumn) => (
        <Switch size="small" checked={row.filterable} disabled={!row.visible} onChange={v => patch(row.dataIndex, { filterable: v })} />
      ),
    },
    {
      title: t('layouts.width', 'Width'),
      key: 'width',
      width: 120,
      render: (_: unknown, row: EditorColumn) => (
        <InputNumber
          size="small"
          min={40}
          max={600}
          value={row.width ?? undefined}
          disabled={!row.visible}
          placeholder={t('layouts.auto', 'auto')}
          onChange={v => patch(row.dataIndex, { width: v ?? null })}
          style={{ width: 90 }}
        />
      ),
    },
  ];

  const hasColumns = columns.length > 0;

  return (
    <div>
      <Space align="center" style={{ marginBottom: 18 }}>
        <LayoutOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
        <Title level={3} style={{ margin: 0 }}>{t('layouts.title', 'Screen layouts')}</Title>
      </Space>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 14 }}
        message={t('layouts.help', 'Reshape any list screen — show, hide, reorder columns and toggle sorting or filtering. Anything you do not customize keeps the shipped layout.')}
        action={
          <Select
            value={selected ?? undefined}
            onChange={setSelected}
            options={entityOptions}
            style={{ minWidth: 220 }}
            placeholder={t('layouts.selectScreen', 'Select a screen')}
          />
        }
      />

      <Card
        loading={loading}
        title={selected ? t(`entity.${selected}`, selected) : t('layouts.selectScreen', 'Select a screen')}
        extra={
          <Space>
            <Tooltip title={isCustomized ? '' : t('layouts.notCustomized', 'This screen uses the default layout')}>
              <Button icon={<UndoOutlined />} onClick={revert} disabled={!isCustomized}>
                {t('layouts.revert', 'Revert to default')}
              </Button>
            </Tooltip>
            <Button type="primary" icon={<SaveOutlined />} onClick={save} loading={saving} disabled={!dirty}>
              {t('layouts.save', 'Save layout')}
            </Button>
          </Space>
        }
      >
        {hasColumns ? (
          <Table<EditorColumn>
            rowKey="dataIndex"
            size="small"
            pagination={false}
            dataSource={columns}
            columns={editorColumns}
          />
        ) : (
          <Empty description={t('layouts.noColumns', 'This screen has no customizable columns.')} />
        )}
      </Card>
    </div>
  );
};

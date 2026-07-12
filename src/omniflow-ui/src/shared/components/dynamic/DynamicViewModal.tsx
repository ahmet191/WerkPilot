import { Modal, Descriptions, Tag, Button, Empty, Typography } from 'antd';
import { EditOutlined } from '@ant-design/icons';
import type { GridColumn } from '../../../core/schema.models';
import { useTranslation } from '../../../core/LanguageContext';

const STATE_COLORS: Record<string, string> = {
  New: 'blue',
  InProgress: 'gold',
  WaitingForApproval: 'purple',
  Resolved: 'green',
  Closed: 'default',
  Pending: 'gold',
  Approved: 'green',
  Rejected: 'red',
  Assigned: 'blue',
  InStock: 'green',
  Broken: 'red',
  Retired: 'default',
};

export function renderTypedValue(value: any, type?: string) {
  if (value === null || value === undefined || value === '') return <Typography.Text type="secondary">—</Typography.Text>;

  switch (type) {
    case 'tag': {
      const text = String(value);
      return <Tag color={STATE_COLORS[text] ?? 'blue'}>{text}</Tag>;
    }
    case 'date': {
      const d = new Date(value);
      return isNaN(d.getTime()) ? String(value) : d.toLocaleString();
    }
    default:
      if (typeof value === 'boolean') return <Tag color={value ? 'green' : 'default'}>{value ? 'Yes' : 'No'}</Tag>;
      if (typeof value === 'object') return <code style={{ fontSize: 12 }}>{JSON.stringify(value)}</code>;
      return String(value);
  }
}

interface DynamicViewModalProps {
  open: boolean;
  record: any | null;
  columns: GridColumn[];
  title?: string;
  onCancel: () => void;
  /** Optional shortcut: closes the view and opens the edit modal for this record. */
  onEdit?: (record: any) => void;
}

/**
 * Schema-driven read-only detail view. Grid columns render first (with their
 * typed formatting); any remaining scalar fields on the record follow, so
 * nothing in the payload is hidden from the user.
 */
export function DynamicViewModal({ open, record, columns, title, onCancel, onEdit }: DynamicViewModalProps) {
  const { t } = useTranslation();

  if (!record) {
    return (
      <Modal open={open} onCancel={onCancel} footer={null} title={title}>
        <Empty />
      </Modal>
    );
  }

  const columnKeys = new Set(columns.map(c => c.dataIndex));
  const extraEntries = Object.entries(record).filter(([key, value]) =>
    !columnKeys.has(key) &&
    key !== 'id' &&
    value !== null &&
    value !== undefined &&
    value !== '' &&
    // DynamicData gets its own section below.
    key !== 'dynamicData'
  );

  const dynamicData: Record<string, any> | null =
    record.dynamicData && typeof record.dynamicData === 'object' ? record.dynamicData : null;

  return (
    <Modal
      open={open}
      onCancel={onCancel}
      title={title ?? t('grid.details', 'Details')}
      width={720}
      footer={[
        onEdit && (
          <Button key="edit" type="primary" icon={<EditOutlined />} onClick={() => onEdit(record)}>
            {t('actions.edit', 'Edit')}
          </Button>
        ),
        <Button key="close" onClick={onCancel}>
          {t('common.close', 'Close')}
        </Button>,
      ]}
    >
      <Descriptions bordered size="small" column={2} style={{ marginTop: 8 }}>
        {columns.map(col => (
          <Descriptions.Item key={col.dataIndex} label={t(col.titleKey, col.dataIndex)}>
            {renderTypedValue(record[col.dataIndex], col.type)}
          </Descriptions.Item>
        ))}
        {extraEntries.map(([key, value]) => (
          <Descriptions.Item key={key} label={t(`fields.${key}`, key)}>
            {renderTypedValue(value)}
          </Descriptions.Item>
        ))}
      </Descriptions>

      {dynamicData && Object.keys(dynamicData).length > 0 && (
        <>
          <Typography.Title level={5} style={{ marginTop: 16 }}>
            {t('grid.dynamicFields', 'Custom Fields')}
          </Typography.Title>
          <Descriptions bordered size="small" column={2}>
            {Object.entries(dynamicData).map(([key, value]) => (
              <Descriptions.Item key={key} label={key}>
                {renderTypedValue(value)}
              </Descriptions.Item>
            ))}
          </Descriptions>
        </>
      )}
    </Modal>
  );
}

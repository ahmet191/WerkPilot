import React from 'react';
import { ApartmentOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { DynamicPage } from '../../shared/components/dynamic/DynamicPage';
import type { GridToolbarAction } from '../../shared/components/datagrid/EnterpriseDataGrid';
import { useTranslation } from '../../core/LanguageContext';

/**
 * Ticket templates, with a "Design workflow" action that opens the visual
 * state-machine editor for the selected template.
 */
export const TemplatesAdminPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();

  const extraActions: GridToolbarAction[] = [
    {
      key: 'workflow',
      label: t('workflow.design', 'Design workflow'),
      icon: <ApartmentOutlined />,
      type: 'primary',
      requiresSelection: 'single',
      onClick: rows => navigate(`/templates/${rows[0].id}/workflow`),
    },
  ];

  return <DynamicPage entityType="templates" extraActions={extraActions} />;
};

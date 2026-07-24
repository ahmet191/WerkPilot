import type { ReactNode } from 'react';
import {
  AppstoreOutlined, AppstoreAddOutlined, AuditOutlined, BankOutlined, BarChartOutlined, CalendarOutlined,
  CheckCircleOutlined, CloudServerOutlined, ContainerOutlined, DashboardOutlined, DollarOutlined,
  EyeOutlined, FieldTimeOutlined, FileTextOutlined, FireOutlined, IdcardOutlined, LaptopOutlined, MenuOutlined,
  SafetyCertificateOutlined, SettingOutlined, SolutionOutlined, SyncOutlined, TeamOutlined,
  ToolOutlined, TranslationOutlined, UserOutlined, UserAddOutlined,
} from '@ant-design/icons';

// Curated icon set for DB-driven menu/notification icons. Using a fixed map
// instead of `import * as Icons` keeps the whole ~1 MB @ant-design/icons package
// out of the bundle — only these are shipped. Unknown names fall back to a
// generic icon, so a tenant-customized menu never renders blank.
const ICONS: Record<string, ReactNode> = {
  AppstoreOutlined: <AppstoreOutlined />,
  AppstoreAddOutlined: <AppstoreAddOutlined />,
  AuditOutlined: <AuditOutlined />,
  BankOutlined: <BankOutlined />,
  BarChartOutlined: <BarChartOutlined />,
  CalendarOutlined: <CalendarOutlined />,
  CheckCircleOutlined: <CheckCircleOutlined />,
  CloudServerOutlined: <CloudServerOutlined />,
  ContainerOutlined: <ContainerOutlined />,
  DashboardOutlined: <DashboardOutlined />,
  DollarOutlined: <DollarOutlined />,
  EyeOutlined: <EyeOutlined />,
  FieldTimeOutlined: <FieldTimeOutlined />,
  FileTextOutlined: <FileTextOutlined />,
  FireOutlined: <FireOutlined />,
  IdcardOutlined: <IdcardOutlined />,
  LaptopOutlined: <LaptopOutlined />,
  MenuOutlined: <MenuOutlined />,
  SafetyCertificateOutlined: <SafetyCertificateOutlined />,
  SettingOutlined: <SettingOutlined />,
  SolutionOutlined: <SolutionOutlined />,
  SyncOutlined: <SyncOutlined />,
  TeamOutlined: <TeamOutlined />,
  ToolOutlined: <ToolOutlined />,
  TranslationOutlined: <TranslationOutlined />,
  UserOutlined: <UserOutlined />,
  UserAddOutlined: <UserAddOutlined />,
};

/** Resolves a stored icon name to an element, falling back to a generic icon. */
export function resolveIcon(name?: string | null): ReactNode {
  if (!name) return null;
  return ICONS[name] ?? <AppstoreOutlined />;
}

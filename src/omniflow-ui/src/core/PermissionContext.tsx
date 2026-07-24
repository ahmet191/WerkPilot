import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import { api } from './api';
import { useAuth } from './AuthContext';

interface PermissionContextType {
  permissions: string[];
  /** True when the signed-in user holds the permission (Administrator holds all). */
  has: (permission: string) => boolean;
  hasAny: (...permissions: string[]) => boolean;
  isLoading: boolean;
  refresh: () => Promise<void>;
}

const PermissionContext = createContext<PermissionContextType | undefined>(undefined);

/**
 * Loads the caller's effective permissions once per session and exposes them to
 * the UI. The server is still the authority — this only decides what to render.
 */
export const PermissionProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const [permissions, setPermissions] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const refresh = useCallback(async () => {
    if (!isAuthenticated) {
      setPermissions([]);
      setIsLoading(false);
      return;
    }
    setIsLoading(true);
    try {
      const res = await api.get('/permissions/mine');
      setPermissions(res.data?.permissions ?? []);
    } catch {
      setPermissions([]);
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated]);

  useEffect(() => { refresh(); }, [refresh]);

  const has = useCallback((permission: string) => permissions.includes(permission), [permissions]);
  const hasAny = useCallback(
    (...required: string[]) => required.some(p => permissions.includes(p)),
    [permissions],
  );

  return (
    <PermissionContext.Provider value={{ permissions, has, hasAny, isLoading, refresh }}>
      {children}
    </PermissionContext.Provider>
  );
};

export const usePermissions = (): PermissionContextType => {
  const context = useContext(PermissionContext);
  if (context === undefined) {
    throw new Error('usePermissions must be used within a PermissionProvider');
  }
  return context;
};

/** Permission keys mirrored from the server catalog (OmniFlow.Domain.Common.Permissions). */
export const Perm = {
  TicketsView: 'tickets.view',
  TicketsCreate: 'tickets.create',
  TicketsEdit: 'tickets.edit',
  TicketsDelete: 'tickets.delete',
  TicketsAssign: 'tickets.assign',
  TicketsTransition: 'tickets.transition',
  LeavesView: 'leaves.view',
  LeavesApprove: 'leaves.approve',
  DirectoryView: 'directory.view',
  AssetsView: 'assets.view',
  AssetsManage: 'assets.manage',
  ApprovalsDecide: 'approvals.decide',
  UsersView: 'users.view',
  UsersManage: 'users.manage',
  AccountsManage: 'accounts.manage',
  TemplatesManage: 'templates.manage',
  WorkflowsManage: 'workflows.manage',
  SlaManage: 'sla.manage',
  CategoriesManage: 'categories.manage',
  ServiceDesksManage: 'servicedesks.manage',
  AuditLogsView: 'auditlogs.view',
  PermissionsManage: 'permissions.manage',
  TranslationsManage: 'translations.manage',
  UiSchemaManage: 'uischema.manage',
} as const;

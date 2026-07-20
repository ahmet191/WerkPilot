import React from 'react';
import { Navigate, useLocation, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { usePermissions } from './PermissionContext';
import { Spin, Result, Button } from 'antd';

interface ProtectedRouteProps {
  /** If set, the user must hold at least one of these roles (Administrator always passes). */
  requiredRoles?: string[];
  /** If set, the user must hold this permission in the tenant's matrix. */
  requiredPermission?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ requiredRoles, requiredPermission }) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const { has, isLoading: permissionsLoading } = usePermissions();
  const location = useLocation();

  if (isLoading || (requiredPermission && permissionsLoading)) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!isAuthenticated) {
    // Redirect to /login, preserving where they were headed.
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  const forbidden = (
    <Result
      status="403"
      title="403"
      subTitle="You don't have permission to view this page."
      extra={<Button type="primary" href="/">Back to dashboard</Button>}
      style={{ marginTop: 80 }}
    />
  );

  if (requiredRoles && requiredRoles.length > 0) {
    const roles = user?.roles ?? [];
    const allowed = roles.includes('Administrator') || requiredRoles.some(r => roles.includes(r));
    if (!allowed) return forbidden;
  }

  if (requiredPermission && !has(requiredPermission)) {
    return forbidden;
  }

  return <Outlet />;
};

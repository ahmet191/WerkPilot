import React from 'react';
import { Navigate, useLocation, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { Spin, Result, Button } from 'antd';

interface ProtectedRouteProps {
  /** If set, the user must hold at least one of these roles (Administrator always passes). */
  requiredRoles?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ requiredRoles }) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  if (isLoading) {
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

  if (requiredRoles && requiredRoles.length > 0) {
    const roles = user?.roles ?? [];
    const allowed = roles.includes('Administrator') || requiredRoles.some(r => roles.includes(r));
    if (!allowed) {
      return (
        <Result
          status="403"
          title="403"
          subTitle="You don't have permission to view this page."
          extra={<Button type="primary" href="/">Back to dashboard</Button>}
          style={{ marginTop: 80 }}
        />
      );
    }
  }

  return <Outlet />;
};

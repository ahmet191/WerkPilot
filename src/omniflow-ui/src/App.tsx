import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AppLayout } from './shared/components/AppLayout';
import { DynamicPage } from './shared/components/dynamic/DynamicPage';

import { DynamicDashboard } from './shared/components/dynamic/DynamicDashboard';
import { ServiceDeskPortal } from './features/tickets/components/ServiceDeskPortal';
import { IsolatedServiceDesk } from './features/tickets/components/IsolatedServiceDesk';
import { EmployeeDirectory } from './features/hr/components/EmployeeDirectory';
import { OrgChart } from './features/hr/components/OrgChart';
import { MyApprovals } from './features/approvals/MyApprovals';

import { AuthProvider } from './core/AuthContext';
import { PermissionProvider, Perm } from './core/PermissionContext';
import { PermissionMatrixPage } from './features/admin/PermissionMatrixPage';
import { ProtectedRoute } from './core/ProtectedRoute';
import { LoginPage } from './features/auth/components/LoginPage';
import { SsoCallback } from './features/auth/components/SsoCallback';
import { ForgotPassword } from './features/auth/components/ForgotPassword';
import { ResetPassword } from './features/auth/components/ResetPassword';
import { SecuritySettings } from './features/account/SecuritySettings';
import { UserAdminPage } from './features/admin/UserAdminPage';
import { ErrorBoundary } from './shared/components/ErrorBoundary';
import { NotFound } from './shared/components/NotFound';

// Management pages that require an administrator (or HR admin, where noted).
function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <PermissionProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/login/sso" element={<SsoCallback />} />
            <Route path="/forgot-password" element={<ForgotPassword />} />
            <Route path="/reset-password" element={<ResetPassword />} />

            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<AppLayout />}>
                {/* Available to any authenticated user */}
                <Route index element={<DynamicDashboard />} />
                <Route path="approvals" element={<MyApprovals />} />
                <Route path="settings/security" element={<SecuritySettings />} />
                <Route path="servicedeskportal" element={<ServiceDeskPortal />} />
                <Route path="servicedesk/:id/tickets" element={<IsolatedServiceDesk />} />
                <Route path="tickets" element={<DynamicPage entityType="tickets" />} />
                <Route path="assets" element={<DynamicPage entityType="assets" />} />
                <Route path="directory" element={<EmployeeDirectory />} />
                <Route path="orgchart" element={<OrgChart />} />
                <Route path="leaves" element={<DynamicPage entityType="leaves" />} />
                <Route path="expenses" element={<DynamicPage entityType="expenses" />} />

                {/* Management surfaces — each gated by the permission it needs,
                    so a tenant can re-shape who reaches them without a deploy. */}
                <Route element={<ProtectedRoute requiredPermission={Perm.ServiceDesksManage} />}>
                  <Route path="servicedesks" element={<DynamicPage entityType="servicedesks" />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.UsersManage} />}>
                  <Route path="departments" element={<DynamicPage entityType="departments" />} />
                  <Route path="users" element={<UserAdminPage />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.AuditLogsView} />}>
                  <Route path="auditlogs" element={<DynamicPage entityType="auditlogs" />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.TemplatesManage} />}>
                  <Route path="templates" element={<DynamicPage entityType="templates" />} />
                  <Route path="menus" element={<DynamicPage entityType="menus" />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.CategoriesManage} />}>
                  <Route path="categories" element={<DynamicPage entityType="categories" />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.SlaManage} />}>
                  <Route path="slas" element={<DynamicPage entityType="slas" />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.PermissionsManage} />}>
                  <Route path="settings/permissions" element={<PermissionMatrixPage />} />
                </Route>

                <Route path="*" element={<NotFound />} />
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
        </PermissionProvider>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;

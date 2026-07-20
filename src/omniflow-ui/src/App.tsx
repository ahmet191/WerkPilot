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
const ADMIN_ROLES = ['Administrator', 'HRAdmin'];

function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
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

                {/* Admin-only management surfaces */}
                <Route element={<ProtectedRoute requiredRoles={ADMIN_ROLES} />}>
                  <Route path="servicedesks" element={<DynamicPage entityType="servicedesks" />} />
                  <Route path="departments" element={<DynamicPage entityType="departments" />} />
                  <Route path="auditlogs" element={<DynamicPage entityType="auditlogs" />} />
                  <Route path="templates" element={<DynamicPage entityType="templates" />} />
                  <Route path="categories" element={<DynamicPage entityType="categories" />} />
                  <Route path="slas" element={<DynamicPage entityType="slas" />} />
                  <Route path="users" element={<UserAdminPage />} />
                  <Route path="menus" element={<DynamicPage entityType="menus" />} />
                </Route>

                <Route path="*" element={<NotFound />} />
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;

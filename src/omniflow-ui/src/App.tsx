import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Spin } from 'antd';

// Core shell stays eager — it's on every page and small.
import { AppLayout } from './shared/components/AppLayout';
import { AuthProvider } from './core/AuthContext';
import { PermissionProvider, Perm } from './core/PermissionContext';
import { ProtectedRoute } from './core/ProtectedRoute';
import { ErrorBoundary } from './shared/components/ErrorBoundary';
import { NotFound } from './shared/components/NotFound';

// Feature pages are code-split: each becomes its own chunk, loaded on demand,
// so the initial bundle no longer ships every screen (and heavy deps like the
// workflow designer's React Flow only download when that route is opened).
// These are named exports, hence the `.then(...)` shim.
const DynamicPage = lazy(() => import('./shared/components/dynamic/DynamicPage').then(m => ({ default: m.DynamicPage })));
const DynamicDashboard = lazy(() => import('./shared/components/dynamic/DynamicDashboard').then(m => ({ default: m.DynamicDashboard })));
const ServiceDeskPortal = lazy(() => import('./features/tickets/components/ServiceDeskPortal').then(m => ({ default: m.ServiceDeskPortal })));
const IsolatedServiceDesk = lazy(() => import('./features/tickets/components/IsolatedServiceDesk').then(m => ({ default: m.IsolatedServiceDesk })));
const TicketDetailPage = lazy(() => import('./features/tickets/components/TicketDetailPage').then(m => ({ default: m.TicketDetailPage })));
const EmployeeDirectory = lazy(() => import('./features/hr/components/EmployeeDirectory').then(m => ({ default: m.EmployeeDirectory })));
const EmployeeCenterPage = lazy(() => import('./features/hr/components/EmployeeCenterPage').then(m => ({ default: m.EmployeeCenterPage })));
const OrgChart = lazy(() => import('./features/hr/components/OrgChart').then(m => ({ default: m.OrgChart })));
const MyApprovals = lazy(() => import('./features/approvals/MyApprovals').then(m => ({ default: m.MyApprovals })));
const ReportsPage = lazy(() => import('./features/reports/ReportsPage').then(m => ({ default: m.ReportsPage })));
const PermissionMatrixPage = lazy(() => import('./features/admin/PermissionMatrixPage').then(m => ({ default: m.PermissionMatrixPage })));
const VisibilityMatrixPage = lazy(() => import('./features/admin/VisibilityMatrixPage').then(m => ({ default: m.VisibilityMatrixPage })));
const OnboardingManagementPage = lazy(() => import('./features/hr/components/OnboardingManagementPage').then(m => ({ default: m.OnboardingManagementPage })));
const PerformanceOkrPage = lazy(() => import('./features/hr/components/PerformanceOkrPage').then(m => ({ default: m.PerformanceOkrPage })));
const SkillsCertificatesPage = lazy(() => import('./features/hr/components/SkillsCertificatesPage').then(m => ({ default: m.SkillsCertificatesPage })));
const LmsTrainingPage = lazy(() => import('./features/hr/components/LmsTrainingPage').then(m => ({ default: m.LmsTrainingPage })));
const PlatformTenantsPage = lazy(() => import('./features/platform/PlatformTenantsPage').then(m => ({ default: m.PlatformTenantsPage })));
const TemplatesAdminPage = lazy(() => import('./features/admin/TemplatesAdminPage').then(m => ({ default: m.TemplatesAdminPage })));
const WorkflowDesignerPage = lazy(() => import('./features/admin/WorkflowDesignerPage').then(m => ({ default: m.WorkflowDesignerPage })));
const TranslationsAdminPage = lazy(() => import('./features/admin/TranslationsAdminPage').then(m => ({ default: m.TranslationsAdminPage })));
const UiSchemaAdminPage = lazy(() => import('./features/admin/UiSchemaAdminPage').then(m => ({ default: m.UiSchemaAdminPage })));
const UserAdminPage = lazy(() => import('./features/admin/UserAdminPage').then(m => ({ default: m.UserAdminPage })));
const SecuritySettings = lazy(() => import('./features/account/SecuritySettings').then(m => ({ default: m.SecuritySettings })));
const LoginPage = lazy(() => import('./features/auth/components/LoginPage').then(m => ({ default: m.LoginPage })));
const SsoCallback = lazy(() => import('./features/auth/components/SsoCallback').then(m => ({ default: m.SsoCallback })));
const ForgotPassword = lazy(() => import('./features/auth/components/ForgotPassword').then(m => ({ default: m.ForgotPassword })));
const ResetPassword = lazy(() => import('./features/auth/components/ResetPassword').then(m => ({ default: m.ResetPassword })));

const PageFallback = () => (
  <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
    <Spin size="large" />
  </div>
);

function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <PermissionProvider>
        <BrowserRouter>
          <Suspense fallback={<PageFallback />}>
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
                <Route path="tickets" element={<DynamicPage entityType="tickets" detailRoute={r => `/tickets/${r.id}`} />} />
                <Route path="tickets/:id" element={<TicketDetailPage />} />
                <Route path="reports" element={<ReportsPage />} />
                <Route path="assets" element={<DynamicPage entityType="assets" />} />
                <Route path="directory" element={<EmployeeDirectory />} />
                <Route path="hr/center" element={<EmployeeCenterPage />} />
                <Route path="hr/onboarding" element={<OnboardingManagementPage />} />
                <Route path="hr/okrs" element={<PerformanceOkrPage />} />
                <Route path="hr/skills" element={<SkillsCertificatesPage />} />
                <Route path="hr/trainings" element={<LmsTrainingPage />} />
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
                  <Route path="templates" element={<TemplatesAdminPage />} />
                  <Route path="templates/:id/workflow" element={<WorkflowDesignerPage />} />
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
                  <Route path="settings/visibility" element={<VisibilityMatrixPage />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.TranslationsManage} />}>
                  <Route path="settings/translations" element={<TranslationsAdminPage />} />
                </Route>
                <Route element={<ProtectedRoute requiredPermission={Perm.UiSchemaManage} />}>
                  <Route path="settings/layouts" element={<UiSchemaAdminPage />} />
                </Route>

                {/* Platform operator surface — above the tenant boundary. */}
                <Route element={<ProtectedRoute requiredRoles={['PlatformAdmin']} />}>
                  <Route path="platform/tenants" element={<PlatformTenantsPage />} />
                </Route>

                <Route path="*" element={<NotFound />} />
              </Route>
            </Route>
          </Routes>
          </Suspense>
        </BrowserRouter>
        </PermissionProvider>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;

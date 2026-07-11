import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from './shared/components/AppLayout';
import { DynamicPage } from './shared/components/dynamic/DynamicPage';

import { DynamicDashboard } from './shared/components/dynamic/DynamicDashboard';
import { ServiceDeskPortal } from './features/tickets/components/ServiceDeskPortal';
import { IsolatedServiceDesk } from './features/tickets/components/IsolatedServiceDesk';
import { EmployeeDirectory } from './features/hr/components/EmployeeDirectory';
import { OrgChart } from './features/hr/components/OrgChart';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<AppLayout />}>
          <Route index element={<DynamicDashboard />} />
          <Route path="servicedeskportal" element={<ServiceDeskPortal />} />
          <Route path="servicedesk/:id/tickets" element={<IsolatedServiceDesk />} />
          <Route path="servicedesks" element={<DynamicPage entityType="servicedesks" />} />
          <Route path="departments" element={<DynamicPage entityType="departments" />} />
          <Route path="assets" element={<DynamicPage entityType="assets" />} />
          <Route path="tickets" element={<DynamicPage entityType="tickets" />} />
          <Route path="directory" element={<EmployeeDirectory />} />
          <Route path="orgchart" element={<OrgChart />} />
          <Route path="leaves" element={<DynamicPage entityType="leaves" />} />
          <Route path="expenses" element={<DynamicPage entityType="expenses" />} />
          <Route path="auditlogs" element={<DynamicPage entityType="auditlogs" />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

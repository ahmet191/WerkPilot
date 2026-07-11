import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from './shared/components/AppLayout';
import { DynamicPage } from './shared/components/dynamic/DynamicPage';

import { DynamicDashboard } from './shared/components/dynamic/DynamicDashboard';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<AppLayout />}>
          <Route index element={<DynamicDashboard />} />
          <Route path="departments" element={<DynamicPage entityType="departments" />} />
          <Route path="assets" element={<DynamicPage entityType="assets" />} />
          <Route path="tickets" element={<DynamicPage entityType="tickets" />} />
          <Route path="leaves" element={<DynamicPage entityType="leaves" />} />
          <Route path="auditlogs" element={<DynamicPage entityType="auditlogs" />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

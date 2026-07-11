import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from './shared/components/AppLayout';
import { DepartmentsGrid } from './features/hr/components/DepartmentsGrid';
import { AssetsGrid } from './features/hr/components/AssetsGrid';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<AppLayout />}>
          <Route index element={<Navigate to="/departments" replace />} />
          <Route path="departments" element={<DepartmentsGrid />} />
          <Route path="assets" element={<AssetsGrid />} />
          <Route path="tickets" element={<div style={{padding: 20}}>Tickets grid coming soon...</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

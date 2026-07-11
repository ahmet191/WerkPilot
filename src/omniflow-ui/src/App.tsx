import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from './shared/components/AppLayout';
import { DynamicPage } from './shared/components/dynamic/DynamicPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<AppLayout />}>
          <Route index element={<Navigate to="/departments" replace />} />
          <Route path="departments" element={<DynamicPage entityType="departments" />} />
          <Route path="assets" element={<DynamicPage entityType="assets" />} />
          <Route path="tickets" element={<DynamicPage entityType="tickets" />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

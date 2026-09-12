import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/AppLayout';
import { LoginPage } from '../pages/LoginPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { RoleHomePage } from '../pages/RoleHomePage';
import { ManagerDashboardPage } from '../features/dashboard/ManagerDashboardPage';
import {
  GuestRoute,
  ProtectedRoute,
  RoleHomeRedirect,
  RoleRoute,
} from '../routing/RouteGuards';

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/app" replace />} />
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<LoginPage />} />
      </Route>
      <Route element={<ProtectedRoute />}>
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<RoleHomeRedirect />} />
          <Route element={<RoleRoute role="SELLER" />}>
            <Route path="seller" element={<RoleHomePage role="SELLER" />} />
          </Route>
          <Route element={<RoleRoute role="BUYER" />}>
            <Route path="buyer" element={<RoleHomePage role="BUYER" />} />
          </Route>
          <Route element={<RoleRoute role="MANAGER" />}>
            <Route path="manager" element={<ManagerDashboardPage />} />
          </Route>
        </Route>
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

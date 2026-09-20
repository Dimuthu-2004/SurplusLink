import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { UserRole } from '../auth/authTypes';
import { FullPageStatus } from '../components/FullPageStatus';
import { roleHomePath } from './roleRoutes';

export function ProtectedRoute() {
  const { status } = useAuth();
  const location = useLocation();
  if (status === 'bootstrapping') {
    return <FullPageStatus message="Restoring your session..." />;
  }
  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }
  return <Outlet />;
}

export function GuestRoute() {
  const { status, user } = useAuth();
  if (status === 'bootstrapping') {
    return <FullPageStatus message="Restoring your session..." />;
  }
  if (status === 'authenticated' && user) {
    return <Navigate to={roleHomePath(user.roles)} replace />;
  }
  return <Outlet />;
}

export function RoleRoute({ role }: { role: UserRole }) {
  const { user } = useAuth();
  if (!user) {
    return <Navigate to="/login" replace />;
  }
  if (!user.roles.includes(role)) {
    return <Navigate to={roleHomePath(user.roles)} replace />;
  }
  return <Outlet />;
}

export function MarketplaceRoute() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (!user.roles.includes('BUYER') && !user.roles.includes('SELLER')) {
    return <Navigate to={roleHomePath(user.roles)} replace />;
  }
  return <Outlet />;
}

export function RoleHomeRedirect() {
  const { user } = useAuth();
  return <Navigate to={user ? roleHomePath(user.roles) : '/login'} replace />;
}

import { type ReactNode } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { roleHomePath, roleLabels } from '../routing/roleRoutes';

export function AppLayout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  if (!user) {
    return null;
  }

  const manager = user.roles.includes('MANAGER');

  const navItem = (to: string, label: string, icon: 'grid' | 'offers' | 'materials' | 'categories' | 'requirements' | 'approvals') => (
    <NavLink to={to} end={to === roleHomePath(user.roles)}>
      <AppIcon name={icon} />
      <span>{label}</span>
    </NavLink>
  );

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link className="brand" to={roleHomePath(user.roles)}>
          <span className="brand-mark" aria-hidden="true">S</span>
          SurplusLink
        </Link>
        <div className="account-summary">
          <span>{user.email}</span>
          <span className="role-badge">{user.roles.map(role => roleLabels[role]).join(' + ')}</span>
          <button className="button button-secondary logout-button" type="button" onClick={logout}>
            <AppIcon name="logout" /> Log out
          </button>
        </div>
      </header>
      <div className="app-body">
        <aside className="side-nav">
          <nav aria-label={user.roles.map(role => roleLabels[role]).join(' + ') + ' navigation'}>
            {navItem(roleHomePath(user.roles), manager ? 'Manager Dashboard' : user.roles.map(role => roleLabels[role]).join(' + ') + ' home', 'grid')}
            {(user.roles.includes('BUYER') || user.roles.includes('SELLER')) && navItem('/app/offers', 'My Offers', 'offers')}
            {manager && (
              <>
                <p className="nav-section-label">Management</p>
                {navItem('/app/manager/materials', 'Material listings', 'materials')}
                {navItem('/app/manager/categories', 'Material categories', 'categories')}
                {navItem('/app/manager/requirements', 'Buyer Requirements', 'requirements')}
                {navItem('/app/manager/approvals', 'Pending Approvals', 'approvals')}
              </>
            )}
          </nav>
        </aside>
        <main className="page-content">
          {location.pathname !== roleHomePath(user.roles) && <Link className="back-link dashboard-back" to={roleHomePath(user.roles)}>? Back to dashboard</Link>}
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function AppIcon({ name }: { name: 'grid' | 'offers' | 'materials' | 'categories' | 'requirements' | 'approvals' | 'logout' }) {
  const paths: Record<string, ReactNode> = {
    grid: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></>,
    offers: <><path d="M4 7h16v12H4z" /><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M4 12h16" /></>,
    materials: <><path d="m12 3 8 4.5v9L12 21l-8-4.5v-9z" /><path d="m4 7.5 8 4.5 8-4.5M12 12v9" /></>,
    categories: <><circle cx="8" cy="8" r="3" /><circle cx="17" cy="8" r="3" /><circle cx="12.5" cy="17" r="3" /></>,
    requirements: <><path d="M6 3h9l4 4v14H6z" /><path d="M15 3v5h5M9 13h6M9 17h6" /></>,
    approvals: <><circle cx="12" cy="12" r="9" /><path d="m8 12 2.5 2.5L16 9" /></>,
    logout: <><path d="M10 5H5v14h5M14 8l4 4-4 4M8 12h10" /></>,
  };
  return <svg className="app-icon" aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>;
}

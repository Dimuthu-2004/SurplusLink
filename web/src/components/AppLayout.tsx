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
          <button className="button button-secondary" type="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <div className="app-body">
        <aside className="side-nav">
          <nav aria-label={user.roles.map(role => roleLabels[role]).join(' + ') + ' navigation'}>
            <NavLink to={roleHomePath(user.roles)} end>
              {user.roles.map(role => roleLabels[role]).join(' + ')} home
            </NavLink>
            {(user.roles.includes('BUYER') || user.roles.includes('SELLER')) && <NavLink to="/app/offers">My Offers</NavLink>}
            {manager && (
              <>
                <NavLink to="/app/manager">Material listings</NavLink>
                <NavLink to="/app/manager/categories">Material categories</NavLink>
                <NavLink to="/app/manager/requirements">Buyer Requirements</NavLink>
                <NavLink to="/app/manager/approvals">Pending Approvals</NavLink>
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

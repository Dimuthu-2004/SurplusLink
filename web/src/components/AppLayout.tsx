import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { roleHomePath, roleLabels } from '../routing/roleRoutes';

export function AppLayout() {
  const { user, logout } = useAuth();
  if (!user) {
    return null;
  }

  const manager = user.role === 'MANAGER';

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link className="brand" to={roleHomePath(user.role)}>
          <span className="brand-mark" aria-hidden="true">S</span>
          SurplusLink
        </Link>
        <div className="account-summary">
          <span>{user.email}</span>
          <span className="role-badge">{roleLabels[user.role]}</span>
          <button className="button button-secondary" type="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <div className="app-body">
        <aside className="side-nav">
          <nav aria-label={roleLabels[user.role] + ' navigation'}>
            <NavLink to={roleHomePath(user.role)} end>
              {roleLabels[user.role]} home
            </NavLink>
            {manager && (
              <>
                <NavLink to="/app/manager">Material listings</NavLink>
                <NavLink to="/app/manager/categories">Material categories</NavLink>
                <NavLink to="/app/manager/requirements">Buyer Requirements</NavLink>
              </>
            )}
          </nav>
        </aside>
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

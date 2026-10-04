import '../pages/manager/managerLayout.css';
import { type ReactNode } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { roleHomePath, roleLabels } from '../routing/roleRoutes';

import { BuyerMarketplaceLayout } from '../pages/buyer/BuyerMarketplaceLayout';
import { LogoutConfirmation } from './LogoutConfirmation';
import { SurplusLinkLogo } from './SurplusLinkLogo';
import { LanguageSelector } from '../i18n/LanguageContext';
import { NotificationBell } from '../features/notifications/NotificationBell';
import { UserAvatar } from './UserAvatar';

export function AppLayout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  if (!user) {
    return null;
  }

  const isBuyerRoute = location.pathname.startsWith('/app/buyer');
  const isOffersRoute = location.pathname.startsWith('/app/offers');
  const isBuyerUser = user.roles.includes('BUYER') && !user.roles.includes('MANAGER');
  const isBuyerNotifications = isBuyerUser && location.pathname.startsWith('/app/notifications');
  const isBuyerProfile = isBuyerUser && location.pathname.startsWith('/app/profile');

  if (isBuyerRoute || (isBuyerUser && isOffersRoute) || isBuyerNotifications || isBuyerProfile) {
    return <BuyerMarketplaceLayout />;
  }

  const manager = user.roles.includes('MANAGER');

  const navItem = (to: string, label: string, icon: 'grid' | 'offers' | 'materials' | 'categories' | 'catalog' | 'requirements' | 'approvals' | 'community') => (
    <NavLink to={to} end={to === roleHomePath(user.roles)}>
      <AppIcon name={icon} />
      <span>{label}</span>
    </NavLink>
  );

  return (
    <div className={manager ? 'app-shell manager-shell' : 'app-shell'}>
      <header className="app-header">
        <Link className="brand" to={roleHomePath(user.roles)}>
          <SurplusLinkLogo className="app-brand-logo" />
        </Link>
        <div className="account-summary" style={{ display: 'flex', alignItems: 'center', gap: '0.85rem' }}>
          {location.pathname === roleHomePath(user.roles) && <LanguageSelector />}
          <NotificationBell />
          <Link
            to="/app/profile"
            className="profile-link"
            title="Profile & Settings"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.5rem', textDecoration: 'none', color: 'inherit' }}
          >
            <UserAvatar user={user} size={32} animated={manager} />
            <span style={{ fontWeight: 600 }}>{user.fullName || user.email}</span>
          </Link>
          <span className="role-badge">{user.roles.map(role => roleLabels[role]).join(' + ')}</span>
          <LogoutConfirmation onLogout={logout}><><AppIcon name="logout" /> Log out</></LogoutConfirmation>
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
                {navItem('/app/manager/catalog', 'Item Catalog', 'catalog')}
                {navItem('/app/manager/requirements', 'Buyer Requirements', 'requirements')}
                {navItem('/app/manager/listing-approvals', 'Seller Listing Approvals', 'approvals')}
                {navItem('/app/manager/requirement-approvals', 'Buyer Requirement Approvals', 'approvals')}
                {navItem('/app/manager/community', 'Community', 'community')}
              </>
            )}
          </nav>
        </aside>
        <main className="page-content">
          {location.pathname !== roleHomePath(user.roles) && <Link className="back-link dashboard-back" to={roleHomePath(user.roles)}><AppIcon name="back" /> Back to dashboard</Link>}
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function AppIcon({ name }: { name: 'grid' | 'offers' | 'materials' | 'categories' | 'catalog' | 'requirements' | 'approvals' | 'community' | 'logout' | 'back' }) {
  const paths: Record<string, ReactNode> = {
    grid: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></>,
    offers: <><path d="M4 7h16v12H4z" /><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M4 12h16" /></>,
    materials: <><path d="m12 3 8 4.5v9L12 21l-8-4.5v-9z" /><path d="m4 7.5 8 4.5 8-4.5M12 12v9" /></>,
    categories: <><circle cx="8" cy="8" r="3" /><circle cx="17" cy="8" r="3" /><circle cx="12.5" cy="17" r="3" /></>,
    catalog: <><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z" /><path d="M6 6h10" /><path d="M6 10h10" /><path d="M6 14h6" /></>,
    requirements: <><path d="M6 3h9l4 4v14H6z" /><path d="M15 3v5h5M9 13h6M9 17h6" /></>,
    approvals: <><circle cx="12" cy="12" r="9" /><path d="m8 12 2.5 2.5L16 9" /></>,
    community: <><circle cx="9" cy="7" r="4" /><path d="M17 11a3 3 0 0 0-3-3M2 21v-2a4 4 0 0 1 4-4h6a4 4 0 0 1 4 4v2M19 21v-1a3 3 0 0 0-2-2.8" /></>,
    back: <path d="m12 5-7 7 7 7M5 12h15" />,
    logout: <><path d="M10 5H5v14h5M14 8l4 4-4 4M8 12h10" /></>,
  };
  return <svg className="app-icon" aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>;
}

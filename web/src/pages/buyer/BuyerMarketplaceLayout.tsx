import { useState, type ReactNode } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { LogoutConfirmation } from '../../components/LogoutConfirmation';
import './buyerMarketplace.css';

export interface BuyerMarketplaceLayoutProps {
  children?: ReactNode;
}

export function BuyerMarketplaceLayout({ children }: BuyerMarketplaceLayoutProps) {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const isMarketplaceActive =
    location.pathname === '/app/buyer' || location.pathname.startsWith('/app/buyer/materials');
  const isActivityActive =
    location.pathname.startsWith('/app/offers') || location.pathname.startsWith('/app/buyer/offers');

  return (
    <div className="buyer-marketplace-shell" data-testid="buyer-unified-layout">
      {/* Shared Unified Header */}
      <header className="marketplace-header">
        <div className="marketplace-nav-container">
          <Link
            to="/app/buyer"
            className="marketplace-brand-link"
            aria-label="SurplusLink Buyer Home"
            onClick={() => setMobileMenuOpen(false)}
          >
            <img
              src="/images/brand/surpluslink-mark.png"
              alt="SurplusLink"
              className="marketplace-brand-logo"
            />
            <span>SurplusLink</span>
          </Link>

          {/* Desktop Nav */}
          <nav className="marketplace-nav-links" aria-label="Buyer Navigation">
            <Link
              to="/app/buyer"
              className={`marketplace-nav-link ${isMarketplaceActive ? 'active' : ''}`}
            >
              Marketplace
            </Link>
            <Link
              to="/app/offers"
              className={`marketplace-nav-link ${isActivityActive ? 'active' : ''}`}
            >
              My Activity
            </Link>
          </nav>

          {/* Desktop User Section */}
          <div className="marketplace-nav-user">
            {user && (
              <div className="marketplace-user-pill">
                <span title={user.email}>{user.email}</span>
                <span className="marketplace-role-tag">Buyer</span>
              </div>
            )}
            <LogoutConfirmation className="marketplace-logout-btn" onLogout={logout}>Log out</LogoutConfirmation>

            {/* Mobile Menu Toggle */}
            <button
              type="button"
              className="marketplace-mobile-menu-btn"
              aria-label={mobileMenuOpen ? 'Close menu' : 'Open menu'}
              aria-expanded={mobileMenuOpen}
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              {mobileMenuOpen ? (
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              ) : (
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <line x1="3" y1="12" x2="21" y2="12" />
                  <line x1="3" y1="6" x2="21" y2="6" />
                  <line x1="3" y1="18" x2="21" y2="18" />
                </svg>
              )}
            </button>
          </div>
        </div>

        {/* Mobile Navigation Drawer */}
        {mobileMenuOpen && (
          <div className="marketplace-mobile-drawer" role="navigation" aria-label="Mobile Navigation">
            <Link
              to="/app/buyer"
              className={`marketplace-mobile-nav-link ${isMarketplaceActive ? 'active' : ''}`}
              onClick={() => setMobileMenuOpen(false)}
            >
              Marketplace
            </Link>
            <Link
              to="/app/offers"
              className={`marketplace-mobile-nav-link ${isActivityActive ? 'active' : ''}`}
              onClick={() => setMobileMenuOpen(false)}
            >
              My Activity
            </Link>
            {user && (
              <div className="marketplace-mobile-user">
                <span>{user.email}</span>
                <span className="marketplace-role-tag">Buyer</span>
              </div>
            )}
            <LogoutConfirmation className="marketplace-logout-btn" onLogout={logout} onOpen={() => setMobileMenuOpen(false)}>Log out</LogoutConfirmation>
          </div>
        )}
      </header>

      {/* Main Outlet/Content Area */}
      <main className="buyer-marketplace-main">
        {children || <Outlet />}
      </main>
    </div>
  );
}

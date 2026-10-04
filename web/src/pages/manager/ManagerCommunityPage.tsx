import React, { useEffect, useState, useCallback } from 'react';
import { PageHeader } from '../../components/DesignSystem';
import { UserAvatar } from '../../components/UserAvatar';
import { managerCommunityApi, type CommunityUser, type CommunityUserDetails } from '../../features/community/managerCommunityApi';
import { requirementDate, requirementNumber } from '../../features/requirements/requirementUi';
import './managerLayout.css';

export function ManagerCommunityPage() {
  const [users, setUsers] = useState<CommunityUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('ALL');
  const [selectedUser, setSelectedUser] = useState<CommunityUserDetails | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(false);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await managerCommunityApi.listUsers(search, roleFilter);
      setUsers(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to load community directory.');
    } finally {
      setLoading(false);
    }
  }, [search, roleFilter]);

  useEffect(() => {
    const timer = setTimeout(() => {
      void fetchUsers();
    }, 250);
    return () => clearTimeout(timer);
  }, [fetchUsers]);

  const openUserDetails = async (user: CommunityUser) => {
    setLoadingDetails(true);
    try {
      const details = await managerCommunityApi.getUserDetails(user.id);
      setSelectedUser(details);
    } catch {
      // Fallback to basic user info if detail lookup fails
      setSelectedUser({
        ...user,
        listingsCount: 0,
        requestsCount: 0,
        completedTransactionsCount: 0,
      });
    } finally {
      setLoadingDetails(false);
    }
  };

  return (
    <div className="manager-page manager-community" data-testid="manager-community-page">
      <PageHeader eyebrow="People & Marketplace Participants" title="Community Directory">
        <p className="muted">
          Read-only directory of verified builders, suppliers, and managers across Sri Lanka.
        </p>
      </PageHeader>

      <section className="manager-panel">
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center', marginBottom: '1.5rem' }}>
          <label style={{ flex: '1 1 240px', minWidth: 200, display: 'flex', alignItems: 'center', gap: '0.5rem', background: '#ffffff', border: '1px solid #cbd5e1', borderRadius: '8px', padding: '0.5rem 0.85rem' }}>
            <span aria-hidden="true" style={{ color: '#94a3b8' }}>🔍</span>
            <input
              type="search"
              placeholder="Search by name, email, business, or phone…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ border: 'none', outline: 'none', width: '100%', fontSize: '0.9rem' }}
              aria-label="Search community members"
            />
          </label>

          <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
            {['ALL', 'BUYER', 'SELLER', 'MANAGER'].map((role) => (
              <button
                key={role}
                type="button"
                className={`button ${roleFilter === role ? 'button-primary' : 'button-secondary'}`}
                style={{ fontSize: '0.8rem', padding: '0.45rem 0.85rem' }}
                onClick={() => setRoleFilter(role)}
              >
                {role === 'ALL' ? 'All Roles' : role.charAt(0) + role.slice(1).toLowerCase() + 's'}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <p className="analytics-loading" role="status">Loading community members…</p>
        ) : error ? (
          <p className="error-message" role="alert">{error}</p>
        ) : users.length === 0 ? (
          <p className="empty-state">No members matched the criteria.</p>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '1rem' }}>
            {users.map((u) => {
              const isManager = u.roles.includes('MANAGER');
              return (
                <article
                  key={u.id}
                  className="manager-panel"
                  style={{
                    padding: '1.25rem',
                    margin: 0,
                    cursor: 'pointer',
                    transition: 'transform 0.15s ease, box-shadow 0.15s ease',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '0.85rem',
                  }}
                  onClick={() => void openUserDetails(u)}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.85rem' }}>
                    <UserAvatar user={u} size={44} animated={isManager} />
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <h3 style={{ margin: 0, fontSize: '0.975rem', fontWeight: 700, color: '#0f172a', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {u.fullName || 'Unnamed Member'}
                      </h3>
                      <p style={{ margin: '0.1rem 0 0 0', fontSize: '0.8rem', color: '#64748b', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {u.businessName || u.email}
                      </p>
                    </div>
                  </div>

                  <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap' }}>
                    {u.roles.map((r) => (
                      <span
                        key={r}
                        style={{
                          fontSize: '0.7rem',
                          fontWeight: 700,
                          padding: '0.15rem 0.5rem',
                          borderRadius: '999px',
                          backgroundColor: r === 'MANAGER' ? '#fef3c7' : r === 'SELLER' ? '#dbeafe' : '#dcfce7',
                          color: r === 'MANAGER' ? '#b45309' : r === 'SELLER' ? '#1d4ed8' : '#15803d',
                        }}
                      >
                        {r}
                      </span>
                    ))}
                    {u.emailVerified && (
                      <span style={{ fontSize: '0.7rem', fontWeight: 600, padding: '0.15rem 0.5rem', borderRadius: '999px', backgroundColor: '#f1f5f9', color: '#475569' }}>
                        ✓ Verified
                      </span>
                    )}
                  </div>

                  <div style={{ marginTop: 'auto', paddingTop: '0.5rem', borderTop: '1px solid #f1f5f9', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>
                      Joined {requirementDate(u.createdAtUtc)}
                    </span>
                    <span style={{ fontSize: '0.8rem', fontWeight: 600, color: '#2563eb' }}>
                      View details →
                    </span>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>

      {/* User Details Modal (Read-Only) */}
      {selectedUser && (
        <div className="modal-backdrop" role="presentation" onClick={() => setSelectedUser(null)}>
          <div
            className="manager-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="user-modal-title"
            style={{ maxWidth: 540, width: '100%', margin: 'auto' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="section-heading" style={{ marginBottom: '1rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                <UserAvatar user={selectedUser} size={54} animated={selectedUser.roles.includes('MANAGER')} />
                <div>
                  <p className="eyebrow" style={{ margin: 0 }}>Community Member</p>
                  <h2 id="user-modal-title" style={{ margin: 0, fontSize: '1.25rem' }}>
                    {selectedUser.fullName || selectedUser.email}
                  </h2>
                </div>
              </div>
              <button type="button" className="text-button" onClick={() => setSelectedUser(null)}>
                ✕ Close
              </button>
            </div>

            <dl className="detail-grid" style={{ marginBottom: '1.5rem' }}>
              <div>
                <dt>Email Address</dt>
                <dd>{selectedUser.email}</dd>
              </div>
              <div>
                <dt>Phone Number</dt>
                <dd>{selectedUser.phoneNumber || 'Not provided'}</dd>
              </div>
              <div>
                <dt>Business / Company</dt>
                <dd>{selectedUser.businessName || 'Individual'}</dd>
              </div>
              <div>
                <dt>Address</dt>
                <dd>{selectedUser.address || 'Not specified'}</dd>
              </div>
              <div>
                <dt>Account Roles</dt>
                <dd>{selectedUser.roles.join(', ')}</dd>
              </div>
              <div>
                <dt>Joined On</dt>
                <dd>{requirementDate(selectedUser.createdAtUtc)}</dd>
              </div>
            </dl>

            <h3 style={{ fontSize: '0.95rem', fontWeight: 700, marginBottom: '0.75rem', color: '#0f172a' }}>
              Marketplace Activity
            </h3>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.75rem', marginBottom: '1.5rem', textAlign: 'center' }}>
              <div style={{ background: '#f8fafc', padding: '0.85rem', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
                <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#0f172a' }}>
                  {requirementNumber(selectedUser.listingsCount)}
                </div>
                <small className="muted">Listings Posted</small>
              </div>
              <div style={{ background: '#f8fafc', padding: '0.85rem', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
                <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#0f172a' }}>
                  {requirementNumber(selectedUser.requestsCount)}
                </div>
                <small className="muted">Requirements</small>
              </div>
              <div style={{ background: '#f8fafc', padding: '0.85rem', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
                <div style={{ fontSize: '1.25rem', fontWeight: 800, color: '#0f172a' }}>
                  {requirementNumber(selectedUser.completedTransactionsCount)}
                </div>
                <small className="muted">Completed Deals</small>
              </div>
            </div>

            <p className="muted" style={{ fontSize: '0.75rem', textAlign: 'center', margin: 0 }}>
              🔒 Community members manage their own account details. Manager view is strictly read-only.
            </p>
          </div>
        </div>
      )}
    </div>
  );
}

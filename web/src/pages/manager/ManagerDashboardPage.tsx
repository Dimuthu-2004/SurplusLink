import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { normalizeApiError } from '../../api/apiClient';
import { AnalyticsMetric } from '../../components/AnalyticsChart';
import { managerDashboardApi } from '../../features/dashboard/managerDashboardApi';
import { requirementNumber } from '../../features/requirements/requirementUi';

export function ManagerDashboardPage() {
  const [userSummary, setUserSummary] = useState<{
    data: { totalUsers: number; sellers: number; buyers: number; dualRoleUsers: number; managers: number } | null;
    error: string | null;
    loading: boolean;
  }>({ data: null, error: null, loading: true });

  useEffect(() => {
    let active = true;
    managerDashboardApi.usersSummary().then(
      (data) => {
        if (active) setUserSummary({ data, error: null, loading: false });
      },
      (error: unknown) => {
        if (active) setUserSummary({ data: null, error: normalizeApiError(error).message, loading: false });
      }
    );
    return () => { active = false; };
  }, []);

  return (
    <div className="manager-page manager-dashboard">
      <header className="page-heading">
        <div>
          <p className="eyebrow">SurplusLink overview</p>
          <h1>Manager Dashboard</h1>
          <p className="muted">Inventory, buyer requirements, matches, and transactions.</p>
        </div>
      </header>

      <section className="manager-panel" aria-label="Community" aria-busy={userSummary.loading}>
        <div className="section-heading">
          <div>
            <p className="eyebrow">People powering reuse</p>
            <h2>Community</h2>
          </div>
          <button type="button" className="button button-secondary" disabled={userSummary.loading}
            onClick={() => {
              setUserSummary((prev) => ({ ...prev, loading: true, error: null }));
              managerDashboardApi.usersSummary().then(
                (data) => setUserSummary({ data, error: null, loading: false }),
                (error) => setUserSummary({ data: null, error: normalizeApiError(error).message, loading: false })
              );
            }}>
            {userSummary.error ? 'Retry' : 'Refresh'} community
          </button>
        </div>
        
        {userSummary.loading && <p className="analytics-loading" role="status">Loading community stats…</p>}
        {userSummary.error && <p className="error-message" role="alert">{userSummary.error}</p>}
        {userSummary.data && (
          <div className="analytics-metrics">
            <AnalyticsMetric label="Total Users" value={userSummary.data.totalUsers} />
            <AnalyticsMetric label="Sellers" value={userSummary.data.sellers} />
            <AnalyticsMetric label="Buyers" value={userSummary.data.buyers} />
            <AnalyticsMetric label="Dual-role Users" value={userSummary.data.dualRoleUsers} />
            <AnalyticsMetric label="Managers" value={userSummary.data.managers} />
          </div>
        )}
      </section>
      <div className="dashboard-grid">
        <AnalyticsPanel title="Inventory" load={managerDashboardApi.inventory}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Active listings" value={data.activeCount} tone="success" />
              <Link className="approval-card" to="/app/manager/materials?status=PENDING_VERIFICATION"><AnalyticsMetric label="Pending Listing Approvals" value={data.listingsByStatus.find(item => item.key === 'PENDING_VERIFICATION')?.count ?? 0} tone="pending" detail="Review listings" /></Link>
              <AnalyticsMetric label="Expiring soon" value={data.expiringListings.length} tone="pending" />
            </div>
            <h3>Category totals</h3>
            <p className="muted">Listing counts across all statuses.</p>
            {data.listingsByCategory.length === 0 ? <p className="empty-state">No listings yet.</p> : (
              <div className="table-scroll" role="region" aria-label="Category totals" tabIndex={0}>
                <table>
                  <caption className="sr-only">Listing totals by category</caption>
                  <thead><tr><th scope="col">Category</th><th scope="col">Listings</th></tr></thead>
                  <tbody>{data.listingsByCategory.map((item) => (
                    <tr key={item.key}><th scope="row">{item.key}</th><td>{requirementNumber(item.count)}</td></tr>
                  ))}</tbody>
                </table>
              </div>
            )}
            <Link className="back-link" to="/app/manager/materials">View material listings</Link>
          </>}
        </AnalyticsPanel>

        <AnalyticsPanel title="Buyer requirements" load={managerDashboardApi.requirements}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Open requirements" value={data.openCount} />
              <AnalyticsMetric label="Requirements awaiting approval" tone="pending" value={
                requirementNumber(data.countsByStatus.find((item) => item.status === 'PENDING_APPROVAL')?.count ?? 0)
              } />
              <AnalyticsMetric label="Upcoming requirement deadlines" value={data.upcomingDeadlineCount} detail="Next 7 days" />
            </div>
            {data.total === 0 && <p className="empty-state">No buyer requirements yet.</p>}
            <Link className="back-link" to="/app/manager/requirements">View buyer requirements</Link>
          </>}
        </AnalyticsPanel>

        <AnalyticsPanel title="Matches" load={managerDashboardApi.matches}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Valid matches" value={data.validCount ?? 'Unavailable'} tone="success" />
              <AnalyticsMetric label="Rejected matches" value={data.rejectedCount ?? 'Unavailable'} tone="failed" />
              <AnalyticsMetric label="Route failures" value={data.routeFailureCount} tone="failed" />
              <AnalyticsMetric label="Average match score" value={data.averageScore == null ? 'Unavailable' : `${requirementNumber(data.averageScore * 100, 1)}%`} />
              <AnalyticsMetric label="Average route distance" value={data.averageDistance == null ? 'Unavailable' : `${requirementNumber(data.averageDistance, 2)} km`} />
            </div>
            {data.total === 0 && <p className="empty-state">No matches yet.</p>}
            <h3>Top rejection reasons</h3>
            {data.topRejectionReasons.length === 0 ? <p className="empty-state">No rejection reasons recorded.</p> : (
              <div className="table-scroll" role="region" aria-label="Top rejection reasons" tabIndex={0}>
                <table>
                  <caption className="sr-only">Top rejection reasons</caption>
                  <thead><tr><th scope="col">Reason</th><th scope="col">Rejected matches</th></tr></thead>
                  <tbody>{data.topRejectionReasons.map((item) => (
                    <tr key={item.reason}><th scope="row">{rejectionReasonLabel(item.reason)}</th><td>{requirementNumber(item.count)}</td></tr>
                  ))}</tbody>
                </table>
              </div>
            )}
          </>}
        </AnalyticsPanel>

        <AnalyticsPanel title="Approval queue" load={managerDashboardApi.approvals}>
          {data => <Link className="approval-card" to="/app/manager/approvals"><AnalyticsMetric label="Pending Match / Requirement Approvals" value={data.total} tone="pending" detail="Review buyer-confirmed workflows" /></Link>}
        </AnalyticsPanel>
        <AnalyticsPanel title="Transactions" load={managerDashboardApi.transactions}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Pending approvals" value={data.pendingApprovalCount} tone="pending" />
              <AnalyticsMetric label="Approved transactions" value={data.approvedCount} tone="success" />
            </div>
            <Link className="back-link" to="/app/manager/approvals">Review pending approvals</Link>
            {data.pendingApprovalCount + data.approvedCount + data.rejectedCount + data.completionCount === 0 && (
              <p className="empty-state">No transactions yet.</p>
            )}
          </>}
        </AnalyticsPanel>
      </div>
    </div>
  );
}

function rejectionReasonLabel(reason: string) {
  return reason
    .toLowerCase()
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

function AnalyticsPanel<T>({ title, load, children }: {
  title: string;
  load: () => Promise<T>;
  children: (data: T) => ReactNode;
}) {
  const [state, setState] = useState<{ data: T | null; error: string | null; loading: boolean }>({
    data: null, error: null, loading: true,
  });
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let active = true;
    setState({ data: null, error: null, loading: true });
    void load().then(
      (data) => { if (active) setState({ data, error: null, loading: false }); },
      (error: unknown) => {
        if (active) setState({ data: null, error: normalizeApiError(error).message, loading: false });
      },
    );
    return () => { active = false; };
  }, [load, revision]);

  return (
    <section className="manager-panel" aria-label={title} aria-busy={state.loading}>
      <div className="section-heading">
        <h2>{title}</h2>
        <button type="button" className="button button-secondary" disabled={state.loading}
          onClick={() => setRevision((value) => value + 1)}>
          {state.error ? 'Retry' : 'Refresh'} {title.toLowerCase()}
        </button>
      </div>
      {state.loading && <p className="analytics-loading" role="status">Loading {title.toLowerCase()}…</p>}
      {state.error && <p className="error-message" role="alert">{state.error}</p>}
      {state.data !== null && children(state.data)}
    </section>
  );
}

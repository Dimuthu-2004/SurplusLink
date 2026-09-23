import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { normalizeApiError } from '../../api/apiClient';
import { AnalyticsMetric } from '../../components/AnalyticsChart';
import { managerDashboardApi } from '../../features/dashboard/managerDashboardApi';
import { requirementNumber } from '../../features/requirements/requirementUi';

export function ManagerDashboardPage() {
  return (
    <div className="manager-page manager-dashboard">
      <header className="page-heading">
        <div>
          <p className="eyebrow">SurplusLink overview</p>
          <h1>Manager Dashboard</h1>
          <p className="muted">Inventory, buyer requirements, matches, and transactions.</p>
        </div>
      </header>

      <div className="dashboard-grid">
        <AnalyticsPanel title="Inventory" load={managerDashboardApi.inventory}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Active listings" value={requirementNumber(data.activeCount)} />
            </div>
              <AnalyticsMetric label="Expiring soon" value={requirementNumber(data.expiringListings.length)} />
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
              <AnalyticsMetric label="Open requirements" value={requirementNumber(data.openCount)} />
              <AnalyticsMetric label="Pending requirements" detail="Pending approval" value={
                requirementNumber(data.countsByStatus.find((item) => item.status === 'PENDING_APPROVAL')?.count ?? 0)
              } />
              <AnalyticsMetric label="Upcoming requirement deadlines" value={requirementNumber(data.upcomingDeadlineCount)} detail="Next 7 days" />
            </div>
            {data.total === 0 && <p className="empty-state">No buyer requirements yet.</p>}
            <Link className="back-link" to="/app/manager/requirements">View buyer requirements</Link>
          </>}
        </AnalyticsPanel>

        <AnalyticsPanel title="Matches" load={managerDashboardApi.matches}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Average match score" value={average(data.averageScore)} />
              <AnalyticsMetric label="Route failures" value={requirementNumber(data.routeFailureCount)} />
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

        <AnalyticsPanel title="Transactions" load={managerDashboardApi.transactions}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Pending approvals" value={requirementNumber(data.pendingApprovalCount)} />
              <AnalyticsMetric label="Approved transactions" value={requirementNumber(data.approvedCount)} />
              <AnalyticsMetric label="Rejected transactions" value={requirementNumber(data.rejectedCount)} />
              <AnalyticsMetric label="Completed transactions" value={requirementNumber(data.completionCount)} />
            </div>
            {data.pendingApprovalCount + data.approvedCount + data.rejectedCount + data.completionCount === 0 && (
              <p className="empty-state">No transactions yet.</p>
            )}
          </>}
        </AnalyticsPanel>
      </div>
    </div>
  );
}

function average(value: number | null) {
  return value === null ? 'Not available' : requirementNumber(value, 2);
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
      {state.loading && <p role="status">Loading {title.toLowerCase()}…</p>}
      {state.error && <p className="error-message" role="alert">{state.error}</p>}
      {state.data !== null && children(state.data)}
    </section>
  );
}

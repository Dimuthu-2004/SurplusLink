import { type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { AnalyticsMetric } from '../../components/AnalyticsChart';
import { LiveIndicator, MetricTrend, PageHeader } from '../../components/DesignSystem';
import { managerDashboardApi } from '../../features/dashboard/managerDashboardApi';
import { requirementNumber } from '../../features/requirements/requirementUi';
import { useLiveResource } from '../../hooks/useLiveResource';

export function ManagerDashboardPage() {
  const userSummary = useLiveResource(managerDashboardApi.usersSummary);

  return (
    <div className="manager-page manager-dashboard">
      <PageHeader eyebrow="SurplusLink overview" title="Manager Dashboard">
        <p className="muted">A live view of inventory, buyer requirements, matches, and transactions.</p>
        <LiveIndicator updatedAt={userSummary.updatedAt} />
      </PageHeader>

      <section className="manager-panel" aria-label="Community" aria-busy={userSummary.loading}>
        <div className="section-heading">
          <div>
            <p className="eyebrow">People powering reuse</p>
            <h2>Community</h2>
          </div>
          <button type="button" className="button button-secondary" disabled={userSummary.loading}
            onClick={() => void userSummary.reload()}>
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
              <AnalyticsMetric label="Completed Transactions" value={data.completionCount} tone="success" detail="Completed buyer requirement/deal groups" />
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
  const state = useLiveResource(load);

  return (
    <section className="manager-panel" aria-label={title} aria-busy={state.loading}>
      <div className="section-heading">
        <h2>{title}</h2>
        <button type="button" className="button button-secondary" disabled={state.loading}
          onClick={() => void state.reload()}>
          {state.error ? 'Retry' : 'Refresh'} {title.toLowerCase()}
        </button>
      </div>
      {state.loading && <p className="analytics-loading" role="status">Loading {title.toLowerCase()}…</p>}
      {state.error && <p className="error-message" role="alert">{state.error}</p>}
      {state.data !== null && <div className="panel-content">{children(state.data)}<MetricTrend label="Refreshes every 10 seconds" /></div>}
    </section>
  );
}

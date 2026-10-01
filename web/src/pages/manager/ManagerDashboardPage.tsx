import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { AnalyticsMetric } from '../../components/AnalyticsChart';
import { LiveIndicator, MetricTrend, PageHeader } from '../../components/DesignSystem';
import { managerDashboardApi } from '../../features/dashboard/managerDashboardApi';
import { requirementNumber } from '../../features/requirements/requirementUi';
import { requirementDate } from '../../features/requirements/requirementUi';
import { transactionConfirmationsApi, type TransactionFollowUp } from '../../features/transactions/transactionsApi';
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
      <TransactionConfirmationWarnings />
      <div className="dashboard-grid">
        <AnalyticsPanel title="Inventory" load={managerDashboardApi.inventory}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Active listings" value={data.activeCount} tone="success" />
              <Link className="approval-card" to="/app/manager/listing-approvals"><AnalyticsMetric label="Pending Listing Approvals" value={data.listingsByStatus.find(item => item.key === 'PENDING_VERIFICATION')?.count ?? 0} tone="pending" detail="Review listings" /></Link>
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
          {data => <Link className="approval-card" to="/app/manager/requirement-approvals"><AnalyticsMetric label="Pending Match / Requirement Approvals" value={data.total} tone="pending" detail="Review buyer-confirmed workflows" /></Link>}
        </AnalyticsPanel>
        <AnalyticsPanel title="Transactions" load={managerDashboardApi.transactions}>
          {(data) => <>
            <div className="analytics-metrics">
              <AnalyticsMetric label="Pending approvals" value={data.pendingApprovalCount} tone="pending" />
              <AnalyticsMetric label="Approved transactions" value={data.approvedCount} tone="success" />
              <AnalyticsMetric label="Completed Transactions" value={data.completionCount} tone="success" detail="Completed buyer requirement/deal groups" />
            </div>
            <Link className="back-link" to="/app/manager/requirement-approvals">Review pending approvals</Link>
            {data.pendingApprovalCount + data.approvedCount + data.rejectedCount + data.completionCount === 0 && (
              <p className="empty-state">No transactions yet.</p>
            )}
          </>}
        </AnalyticsPanel>
      </div>
    </div>
  );
}

function TransactionConfirmationWarnings() {
  const warnings = useLiveResource(transactionConfirmationsApi.followUps);
  const [reviewing, setReviewing] = useState<TransactionFollowUp | null>(null);
  const [busy, setBusy] = useState(false);
  const resolve = async (completed: boolean) => {
    if (!reviewing) return;
    const verb = completed ? 'completed' : 'not completed';
    if (!window.confirm(`Mark this transaction as ${verb} after confirming with the parties?`)) return;
    const note = window.prompt(completed ? 'Resolution note (optional, max 500 characters)' : 'Reason for marking not completed (required, max 500 characters)');
    if (!completed && !note?.trim()) return;
    setBusy(true);
    try {
      if (completed) await transactionConfirmationsApi.resolveCompleted(reviewing.id, note ?? undefined);
      else await transactionConfirmationsApi.resolveNotCompleted(reviewing.id, note!.trim());
      setReviewing(null);
      await warnings.reload();
    } finally { setBusy(false); }
  };
  if (warnings.loading || warnings.error || warnings.data === null) return null;
  return <section className="manager-panel" aria-label="Transaction confirmation warnings">
    <div className="section-heading"><div><p className="eyebrow">Action required</p><h2>⚠ Transaction confirmation pending</h2></div><button type="button" className="button button-secondary" onClick={() => void warnings.reload()}>Refresh</button></div>
    {warnings.data.length === 0 ? <p className="muted">No transaction confirmations currently need attention.</p> : <div className="transaction-warning-grid">{warnings.data.map(row => <article key={row.id} className="transaction-warning-card"><p className="eyebrow">{row.status === 'MANAGER_REVIEW_REQUIRED' ? 'Manager review required' : 'Follow-up needed'}</p><h3>{row.materialTitle}</h3><p className="muted">{row.reference} · Seller: {row.seller.fullName || 'Unavailable'}</p><p>Seller handover: {row.sellerHandoverConfirmedAt ? 'Confirmed' : 'Not confirmed'}</p><p>Buyer receipt: {row.buyerReceivedConfirmedAt ? 'Received' : row.sellerHandoverConfirmedAt ? 'No response' : 'Waiting for seller'}</p><button type="button" className="button" onClick={() => setReviewing(row)}>Review transaction</button></article>)}</div>}
    {reviewing && <div className="modal-backdrop" role="presentation"><section className="manager-panel" role="dialog" aria-modal="true" aria-label="Transaction confirmation review"><div className="section-heading"><h2>{reviewing.reference} · {reviewing.materialTitle}</h2><button type="button" className="text-button" onClick={() => setReviewing(null)}>Close</button></div>
      <dl className="detail-grid"><div><dt>Seller</dt><dd>{reviewing.seller.fullName || 'Seller unavailable'}</dd></div><div><dt>Buyer</dt><dd>{reviewing.buyer.fullName || 'Buyer unavailable'}</dd></div><div><dt>Physical selected quantity</dt><dd>{requirementNumber(reviewing.quantity)} {reviewing.unit}</dd></div><div><dt>Material value</dt><dd>LKR {requirementNumber(reviewing.totalValue, 2)}</dd></div><div><dt>Approved</dt><dd>{reviewing.managerApprovedAt ? requirementDate(reviewing.managerApprovedAt) : 'Unavailable'}</dd></div><div><dt>Confirmation deadline</dt><dd>{reviewing.confirmationDeadline ? requirementDate(reviewing.confirmationDeadline) : 'Unavailable'} ({reviewing.daysRemaining} days remaining)</dd></div><div><dt>Seller handover</dt><dd>{reviewing.sellerHandoverConfirmedAt ? `Confirmed — ${requirementDate(reviewing.sellerHandoverConfirmedAt)}` : 'Not confirmed'}</dd></div><div><dt>Buyer receipt</dt><dd>{reviewing.buyerReceivedConfirmedAt ? `Received — ${requirementDate(reviewing.buyerReceivedConfirmedAt)}` : reviewing.sellerHandoverConfirmedAt ? 'No response' : 'Waiting for Seller'}</dd></div></dl>
      <p>Seller phone: {reviewing.seller.phoneNumber || 'Phone number is unavailable.'}</p><p>Buyer phone: {reviewing.buyer.phoneNumber || 'Phone number is unavailable.'}</p><p>Seller email: {reviewing.seller.email}</p><p>Buyer email: {reviewing.buyer.email}</p><div className="button-row"><button type="button" className="button" disabled={busy} onClick={() => void resolve(true)}>Mark Completed</button><button type="button" className="button button-danger" disabled={busy} onClick={() => void resolve(false)}>Mark Not Completed</button></div>
    </section></div>}
  </section>;
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

import { useState, useCallback, useEffect, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { AnalyticsMetric } from '../../components/AnalyticsChart';
import { LiveIndicator, MetricTrend, PageHeader } from '../../components/DesignSystem';
import { managerDashboardApi } from '../../features/dashboard/managerDashboardApi';
import { requirementNumber } from '../../features/requirements/requirementUi';
import {
  transactionConfirmationsApi,
  type TransactionTimeSeriesResponse,
} from '../../features/transactions/transactionsApi';
import { useLiveResource } from '../../hooks/useLiveResource';
import { AnimatedTransactionChart } from '../../components/AnimatedTransactionChart';
import { formatLkr } from '../../utils/currency';

export function ManagerDashboardPage() {
  const userSummary = useLiveResource(managerDashboardApi.usersSummary);

  return (
    <div className="manager-page manager-dashboard" data-testid="manager-dashboard-page">
      <PageHeader eyebrow="SurplusLink overview" title="Manager Dashboard">
        <p className="muted">A live view of inventory, buyer requirements, matches, and transactions.</p>
        <LiveIndicator updatedAt={userSummary.updatedAt} />
      </PageHeader>

      {/* TOP HIERARCHY 1: Approval Queue & Action Required */}
      <div className="dashboard-grid top-actions-grid" style={{ marginBottom: '1.5rem' }}>
        <AnalyticsPanel title="Approval queue" load={managerDashboardApi.approvals}>
          {(data) => (
            <Link className="approval-card" to="/app/manager/requirement-approvals">
              <AnalyticsMetric
                label="Pending Match / Requirement Approvals"
                value={data.total}
                tone="pending"
                detail="Review buyer-confirmed workflows"
              />
            </Link>
          )}
        </AnalyticsPanel>

        <AnalyticsPanel title="Transactions" load={managerDashboardApi.transactions}>
          {(data) => (
            <>
              <div className="analytics-metrics">
                <AnalyticsMetric label="Pending approvals" value={data.pendingApprovalCount} tone="pending" />
                <AnalyticsMetric label="Approved transactions" value={data.approvedCount} tone="success" />
                <AnalyticsMetric
                  label="Completed Transactions"
                  value={data.completionCount}
                  tone="success"
                  detail="Completed buyer requirement/deal groups"
                />
              </div>
              <Link className="back-link" to="/app/manager/requirement-approvals">
                Review pending approvals
              </Link>
              {data.pendingApprovalCount + data.approvedCount + data.rejectedCount + data.completionCount === 0 && (
                <p className="empty-state">No transactions yet.</p>
              )}
            </>
          )}
        </AnalyticsPanel>
      </div>

      <TransactionFollowUps />

      {/* HIERARCHY 2: Core KPIs */}
      <div className="dashboard-grid kpis-grid" style={{ marginBottom: '1.5rem' }}>
        <AnalyticsPanel title="Inventory" load={managerDashboardApi.inventory}>
          {(data) => (
            <>
              <div className="analytics-metrics">
                <AnalyticsMetric label="Active listings" value={data.activeCount} tone="success" />
                <Link className="approval-card" to="/app/manager/listing-approvals">
                  <AnalyticsMetric
                    label="Pending Listing Approvals"
                    value={data.listingsByStatus.find((item) => item.key === 'PENDING_VERIFICATION')?.count ?? 0}
                    tone="pending"
                    detail="Review listings"
                  />
                </Link>
                <AnalyticsMetric label="Expiring soon" value={data.expiringListings.length} tone="pending" />
              </div>
              <h3>Category totals</h3>
              <p className="muted">Listing counts across all statuses.</p>
              {data.listingsByCategory.length === 0 ? (
                <p className="empty-state">No listings yet.</p>
              ) : (
                <div className="table-scroll" role="region" aria-label="Category totals" tabIndex={0}>
                  <table>
                    <caption className="sr-only">Listing totals by category</caption>
                    <thead>
                      <tr>
                        <th scope="col">Category</th>
                        <th scope="col">Listings</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.listingsByCategory.map((item) => (
                        <tr key={item.key}>
                          <th scope="row">{item.key}</th>
                          <td>{requirementNumber(item.count)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <Link className="back-link" to="/app/manager/materials">
                View material listings
              </Link>
            </>
          )}
        </AnalyticsPanel>

        <AnalyticsPanel title="Buyer requirements" load={managerDashboardApi.requirements}>
          {(data) => (
            <>
              <div className="analytics-metrics">
                <AnalyticsMetric label="Open requirements" value={data.openCount} />
                <AnalyticsMetric
                  label="Requirements awaiting approval"
                  tone="pending"
                  value={
                    requirementNumber(
                      data.countsByStatus.find((item) => item.status === 'PENDING_APPROVAL')?.count ?? 0
                    )
                  }
                />
                <AnalyticsMetric
                  label="Upcoming requirement deadlines"
                  value={data.upcomingDeadlineCount}
                  detail="Next 7 days"
                />
              </div>
              {data.total === 0 && <p className="empty-state">No buyer requirements yet.</p>}
              <Link className="back-link" to="/app/manager/requirements">
                View buyer requirements
              </Link>
            </>
          )}
        </AnalyticsPanel>

        <AnalyticsPanel title="Matches" load={managerDashboardApi.matches}>
          {(data) => (
            <>
              <div className="analytics-metrics">
                <AnalyticsMetric label="Valid matches" value={data.validCount ?? 'Unavailable'} tone="success" />
                <AnalyticsMetric label="Rejected matches" value={data.rejectedCount ?? 'Unavailable'} tone="failed" />
                <AnalyticsMetric label="Route failures" value={data.routeFailureCount} tone="failed" />
                <AnalyticsMetric
                  label="Average match score"
                  value={
                    data.averageScore == null
                      ? 'No match score data yet'
                      : `${requirementNumber(data.averageScore * 100, 1)}%`
                  }
                />
                <AnalyticsMetric
                  label="Average route distance"
                  value={
                    data.averageDistance == null
                      ? 'No route data yet'
                      : `${requirementNumber(data.averageDistance, 2)} km`
                  }
                />
              </div>
              {data.total === 0 && <p className="empty-state">No matches yet.</p>}
              <h3>Top rejection reasons</h3>
              {data.topRejectionReasons.length === 0 ? (
                <p className="empty-state">No rejection reasons recorded.</p>
              ) : (
                <div className="table-scroll" role="region" aria-label="Top rejection reasons" tabIndex={0}>
                  <table>
                    <caption className="sr-only">Top rejection reasons</caption>
                    <thead>
                      <tr>
                        <th scope="col">Reason</th>
                        <th scope="col">Rejected matches</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.topRejectionReasons.map((item) => (
                        <tr key={item.reason}>
                          <th scope="row">{rejectionReasonLabel(item.reason)}</th>
                          <td>{requirementNumber(item.count)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </AnalyticsPanel>
      </div>

      {/* HIERARCHY 3: Animated Transaction Analytics Graph */}
      <AnimatedTransactionAnalyticsSection />

      {/* HIERARCHY 4: Secondary Info & Community Overview */}
      <section className="manager-panel" aria-label="Community" aria-busy={userSummary.loading} style={{ marginTop: '1.5rem' }}>
        <div className="section-heading">
          <div>
            <p className="eyebrow">People powering reuse</p>
            <h2>Community</h2>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <Link to="/app/manager/community" className="button button-primary" style={{ fontSize: '0.85rem' }}>
              View Member Directory →
            </Link>
            <button
              type="button"
              className="button button-secondary"
              disabled={userSummary.loading}
              onClick={() => void userSummary.reload()}
            >
              {userSummary.error ? 'Retry' : 'Refresh'} community
            </button>
          </div>
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
    </div>
  );
}

function TransactionFollowUps() {
  const [rows, setRows] = useState<Awaited<ReturnType<typeof transactionConfirmationsApi.followUps>>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  useEffect(() => { transactionConfirmationsApi.followUps().then(setRows).catch(() => setError('Unable to load transaction follow-ups.')).finally(() => setLoading(false)); }, []);
  return <section className="manager-panel" aria-label="Transaction follow-ups" aria-busy={loading} style={{ marginBottom: '1.5rem' }}><div className="section-heading"><h2>Transaction follow-ups</h2></div>{loading && <p className="analytics-loading" role="status">Loading transaction follow-ups…</p>}{error && <p className="error-message" role="alert">{error}</p>}{!loading && !error && rows.length === 0 && <p className="empty-state">No transaction follow-ups need attention.</p>}{!loading && rows.length > 0 && <div className="table-scroll"><table><thead><tr><th>Reference</th><th>Material</th><th>Status</th><th>Deadline</th><th>Handover / receipt</th></tr></thead><tbody>{rows.map(row => <tr key={row.id}><td><Link to={`/app/manager/transactions/${row.id}`}>{row.reference}</Link></td><td>{row.materialTitle}</td><td>{followUpStatusLabel(row.status)}</td><td>{row.confirmationDeadline ? `${row.daysRemaining} days remaining` : 'Not recorded'}</td><td>{row.sellerHandoverConfirmedAt ? 'Seller handed over' : 'Awaiting seller'} / {row.buyerReceivedConfirmedAt ? 'Buyer received' : 'Awaiting buyer'}</td></tr>)}</tbody></table></div>}</section>;
}

function followUpStatusLabel(status: string) {
  return status === 'UNKNOWN' ? 'Unknown status' : status.split('_').map(word => word.charAt(0) + word.slice(1).toLowerCase()).join(' ');
}

function AnimatedTransactionAnalyticsSection() {
  const currentYear = new Date().getFullYear();
  const [selectedYear, setSelectedYear] = useState<number>(currentYear);
  const [selectedMonth, setSelectedMonth] = useState<number | undefined>(undefined);
  const [data, setData] = useState<TransactionTimeSeriesResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await transactionConfirmationsApi.timeseries(selectedYear, selectedMonth);
      setData(res);
    } catch {
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [selectedYear, selectedMonth]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' },
  ];

  return (
    <section className="manager-panel" aria-label="Transaction analytics" style={{ marginTop: '1.5rem' }}>
      <div className="section-heading" style={{ flexWrap: 'wrap', gap: '0.75rem' }}>
        <div>
          <p className="eyebrow">Financial & Material Velocity</p>
          <h2>Completed Transactions Analytics</h2>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <select
            value={selectedYear}
            onChange={(e) => setSelectedYear(Number(e.target.value))}
            aria-label="Filter transactions by year"
            style={{ padding: '0.4rem 0.65rem', borderRadius: '6px', border: '1px solid #cbd5e1', fontSize: '0.85rem' }}
          >
            <option value={currentYear}>{currentYear}</option>
            <option value={currentYear - 1}>{currentYear - 1}</option>
            <option value={currentYear - 2}>{currentYear - 2}</option>
          </select>

          <select
            value={selectedMonth ?? ''}
            onChange={(e) => setSelectedMonth(e.target.value ? Number(e.target.value) : undefined)}
            aria-label="Filter transactions by month"
            style={{ padding: '0.4rem 0.65rem', borderRadius: '6px', border: '1px solid #cbd5e1', fontSize: '0.85rem' }}
          >
            <option value="">All Months</option>
            {months.map((m) => (
              <option key={m.value} value={m.value}>
                {m.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {loading && <p className="analytics-loading">Loading transaction analytics…</p>}
      {error && <p className="error-message">{error}</p>}

      {data && (
        <>
          <div className="analytics-metrics" style={{ marginBottom: '1.25rem' }}>
            <AnalyticsMetric label="Completed Deals" value={data.totalTransactions} tone="success" />
            <AnalyticsMetric label="Total Material Value" value={formatLkr(data.totalValue)} tone="success" />
            <AnalyticsMetric label="Average Deal Value" value={formatLkr(data.averageValue)} />
          </div>

          <AnimatedTransactionChart points={data.points} height={250} />
        </>
      )}
    </section>
  );
}

function rejectionReasonLabel(reason: string) {
  return reason
    .toLowerCase()
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

function AnalyticsPanel<T>({
  title,
  load,
  children,
}: {
  title: string;
  load: () => Promise<T>;
  children: (data: T) => ReactNode;
}) {
  const state = useLiveResource(load);

  return (
    <section className="manager-panel" aria-label={title} aria-busy={state.loading}>
      <div className="section-heading">
        <h2>{title}</h2>
        <button
          type="button"
          className="button button-secondary"
          disabled={state.loading}
          onClick={() => void state.reload()}
        >
          {state.error ? 'Retry' : 'Refresh'} {title.toLowerCase()}
        </button>
      </div>
      {state.loading && <p className="analytics-loading" role="status">Loading {title.toLowerCase()}…</p>}
      {state.error && <p className="error-message" role="alert">{state.error}</p>}
      {state.data !== null && (
        <div className="panel-content">
          {children(state.data)}
          <MetricTrend label="Refreshes every 10 seconds" />
        </div>
      )}
    </section>
  );
}

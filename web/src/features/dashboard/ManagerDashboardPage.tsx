import { useEffect, useState } from 'react';
import { normalizeApiError } from '../../api/apiClient';
import {
  getManagerDashboard,
  type ManagerDashboardData,
} from './managerDashboardApi';

type DashboardState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: ManagerDashboardData };

export function ManagerDashboardPage() {
  const [state, setState] = useState<DashboardState>({ status: 'loading' });
  const [requestVersion, setRequestVersion] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setState({ status: 'loading' });
    getManagerDashboard(controller.signal)
      .then((data) => setState({ status: 'ready', data }))
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState({ status: 'error', message: normalizeApiError(error).message });
        }
      });
    return () => controller.abort();
  }, [requestVersion]);

  if (state.status === 'loading') {
    return <DashboardStatus message="Loading manager analytics..." />;
  }
  if (state.status === 'error') {
    return (
      <section className="dashboard-state" role="alert">
        <h1>Manager dashboard unavailable</h1>
        <p>{state.message}</p>
        <button
          className="button button-primary"
          type="button"
          onClick={() => setRequestVersion((version) => version + 1)}
        >
          Try again
        </button>
      </section>
    );
  }

  const { listings, requirements, matches, transactions } = state.data;
  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      <div className="dashboard-heading">
        <div>
          <p className="eyebrow">Live component summaries</p>
          <h1 id="dashboard-title">Manager dashboard</h1>
        </div>
        <button
          className="button button-secondary"
          type="button"
          onClick={() => setRequestVersion((version) => version + 1)}
        >
          Refresh
        </button>
      </div>

      <section aria-labelledby="inventory-heading">
        <h2 id="inventory-heading">Inventory</h2>
        <div className="metric-grid">
          <MetricCard label="Active listings" value={formatNumber(listings.activeListings)} />
        </div>
        <DataTable
          caption="Listings by category"
          headers={['Category', 'Listings', 'Total quantity']}
          emptyMessage="No listing category totals are available."
          rows={listings.categoryTotals.map((total) => [
            total.categoryName,
            formatNumber(total.listingCount),
            formatNumber(total.totalQuantity),
          ])}
        />
      </section>

      <section aria-labelledby="requirements-heading">
        <h2 id="requirements-heading">Requirements</h2>
        <div className="metric-grid">
          <MetricCard label="Open requirements" value={formatNumber(requirements.openRequirements)} />
          <MetricCard label="Pending requirements" value={formatNumber(requirements.pendingRequirements)} />
        </div>
        <DataTable
          caption="Upcoming requirement deadlines"
          headers={['Requirement', 'Category', 'Deadline', 'Status']}
          emptyMessage="No upcoming requirement deadlines."
          rows={requirements.upcomingDeadlines.map((requirement) => [
            requirement.title,
            requirement.categoryName,
            formatDate(requirement.deadlineUtc),
            formatStatus(requirement.status),
          ])}
        />
      </section>

      <section aria-labelledby="matching-heading">
        <h2 id="matching-heading">Matching</h2>
        <div className="metric-grid">
          <MetricCard label="Average match score" value={formatScore(matches.averageScore)} />
          <MetricCard label="Average distance" value={formatDistance(matches.averageDistanceKm)} />
        </div>
        <DataTable
          caption="Top rejection reasons"
          headers={['Reason', 'Matches']}
          emptyMessage="No match rejection reasons have been recorded."
          rows={matches.topRejectionReasons.map((reason) => [
            reason.reason,
            formatNumber(reason.count),
          ])}
        />
      </section>

      <section aria-labelledby="transactions-heading">
        <h2 id="transactions-heading">Approvals and transactions</h2>
        <div className="metric-grid">
          <MetricCard label="Pending approvals" value={formatNumber(transactions.pendingApprovals)} />
          <MetricCard label="Approved" value={formatNumber(transactions.approvedTransactions)} />
          <MetricCard label="Rejected" value={formatNumber(transactions.rejectedTransactions)} />
          <MetricCard label="Completed" value={formatNumber(transactions.completedTransactions)} />
        </div>
      </section>
    </section>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  );
}

interface DataTableProps {
  caption: string;
  headers: string[];
  rows: string[][];
  emptyMessage: string;
}

function DataTable({ caption, headers, rows, emptyMessage }: DataTableProps) {
  return (
    <div className="table-wrap">
      <table>
        <caption>{caption}</caption>
        <thead>
          <tr>{headers.map((header) => <th key={header} scope="col">{header}</th>)}</tr>
        </thead>
        <tbody>
          {rows.length ? rows.map((row, rowIndex) => (
            <tr key={`${row[0]}-${rowIndex}`}>
              {row.map((cell, cellIndex) => <td key={`${cellIndex}-${cell}`}>{cell}</td>)}
            </tr>
          )) : (
            <tr><td className="empty-cell" colSpan={headers.length}>{emptyMessage}</td></tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function DashboardStatus({ message }: { message: string }) {
  return (
    <section className="dashboard-state" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <p>{message}</p>
    </section>
  );
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat().format(value);
}

function formatScore(value: number | null): string {
  return value === null ? 'No data' : `${(value * 100).toFixed(1)}%`;
}

function formatDistance(value: number | null): string {
  return value === null ? 'No data' : `${value.toFixed(1)} km`;
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
}

function formatStatus(value: string): string {
  return value.toLowerCase().replaceAll('_', ' ');
}

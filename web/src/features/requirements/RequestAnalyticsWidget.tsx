import { AnalyticsChart, AnalyticsMetric } from '../../components/AnalyticsChart';
import { useCallback, useState } from 'react';
import { Link } from 'react-router-dom';
import { managerRequirementsApi, type ManagerRequirementsApi } from './managerRequirementsApi';
import { RequirementError, requirementDate, requirementNumber, statusLabel, useRequirementResource } from './requirementUi';

export function RequestAnalyticsWidget({ api = managerRequirementsApi }: { api?: ManagerRequirementsApi }) {
  const [days, setDays] = useState(7);
  const load = useCallback(() => api.summary(days), [api, days]);
  const { data, error, loading, reload } = useRequirementResource(load);
  return <section className="manager-panel" aria-labelledby="request-analytics-heading">
    <div className="section-heading">
      <div><p className="eyebrow">All buyer requests</p><h2 id="request-analytics-heading">Request analytics</h2></div>
      <label className="requirement-window">Upcoming deadlines
        <select value={days} onChange={(event) => setDays(Number(event.target.value))}>
          {[1, 7, 30, 90].map((value) => <option key={value} value={value}>Next {value} day{value === 1 ? '' : 's'}</option>)}
        </select>
      </label>
    </div>
    {loading && <p role="status">Loading request analytics…</p>}
    {error && <RequirementError message={error} retry={reload} label="Retry analytics" />}
    {data && <>
      <div className="analytics-metrics">
        <AnalyticsMetric label="Total requests" value={requirementNumber(data.total)} />
        <AnalyticsMetric label="Open requests" value={requirementNumber(data.openCount)} />
        <AnalyticsMetric label="Upcoming deadlines" value={requirementNumber(data.upcomingDeadlineCount)} />
        <AnalyticsMetric label="Average maximum budget" value={data.averageMaximumBudget === null ? '—' : requirementNumber(data.averageMaximumBudget, 2)} />
      </div>
      {data.total === 0 && <p className="empty-state">No buyer requirements yet.</p>}
      <div className="analytics-grid">
        <AnalyticsChart title="Requests by status" initialView="ring" values={data.countsByStatus.map(item => ({ key: statusLabel(item.status), count: item.count }))} />
        <AnalyticsChart title="Requests by category" values={data.countsByCategory.map(item => ({ key: item.categoryName, count: item.count }))} />
      </div>
      <details className="requirement-breakdown">
        <summary>View status, category and deadline breakdown</summary>
        <div className="analytics-grid">
          <div><h3>By status</h3><ul className="summary-list">
            {data.countsByStatus.map((item) => <li key={item.status}>{statusLabel(item.status)}: {item.count}</li>)}
          </ul></div>
          <div><h3>By category</h3>{data.countsByCategory.length === 0 ? <p>No categories yet.</p> : <ul className="summary-list">
            {data.countsByCategory.map((item) => <li key={item.categoryId}>{item.categoryName}: {item.count}</li>)}
          </ul>}</div>
          <div><h3>Average quantity by unit</h3>{data.averageQuantityByUnit.length === 0 ? <p>No quantities yet.</p> : <ul className="summary-list">
            {data.averageQuantityByUnit.map((item) => <li key={item.unit}>{requirementNumber(item.averageRequiredQuantity)} {item.unit} ({item.count} requests)</li>)}
          </ul>}</div>
        </div>
        <h3>Upcoming deadlines</h3>
        <p className="muted">Active requests due by {requirementDate(data.upcomingUntil)}. Showing up to 10.</p>
        {data.upcomingDeadlines.length === 0 ? <p>No active requests are due in this window.</p> :
          <ul className="summary-list">{data.upcomingDeadlines.map((item) => <li key={item.id}>
            <Link to={'/app/manager/requirements/' + item.id}>Request {item.id.slice(0, 8)}</Link>
            {' — '}{requirementDate(item.deadline)} · {statusLabel(item.status)}
          </li>)}</ul>}
      </details>
      <div className="section-heading requirement-refresh"><small className="muted">
        As of {requirementDate(data.asOf)}. Analytics covers all requests, independently of table filters.
      </small><button className="text-button" type="button" onClick={reload}>Refresh analytics</button></div>
    </>}
  </section>;
}

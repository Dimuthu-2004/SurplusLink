import { useCallback } from 'react';
import { managerMatchesApi, type ManagerMatchesApi } from './managerMatchesApi';
import { RequirementError, requirementNumber, useRequirementResource } from '../requirements/requirementUi';

export function MatchAnalyticsWidget({ api = managerMatchesApi, requirementId }: { api?: ManagerMatchesApi; requirementId?: string }) {
  const load = useCallback(() => api.summary(requirementId ?? 'all'), [api, requirementId]);
  const { data, error, loading, reload } = useRequirementResource(load);

  return <section className="manager-panel" aria-labelledby="match-analytics-heading">
    <div className="section-heading">
      <div><p className="eyebrow">Comparison analytics</p><h2 id="match-analytics-heading">Match analytics</h2></div>
      <small className="muted">All recorded candidates</small>
    </div>
    {loading && <p role="status">Loading match analytics…</p>}
    {error && <RequirementError message={error} retry={reload} label="Retry" />}
    {data && <>
      <div className="analytics-metrics">
        <div className="metric-box"><span>Total candidates</span><strong>{requirementNumber(data.total)}</strong></div>
        <div className="metric-box"><span>Valid</span><strong>{requirementNumber(data.validCount)}</strong></div>
        <div className="metric-box"><span>Rejected</span><strong>{requirementNumber(data.rejectedCount)}</strong></div>
        <div className="metric-box"><span>Avg score</span><strong>{data.averageScore === null ? '—' : requirementNumber(data.averageScore, 1)}</strong></div>
      </div>
      <div className="analytics-grid">
        <div><h3>Cost and route</h3><ul className="summary-list">
          <li>Average route: {data.averageRouteKm === null ? '—' : requirementNumber(data.averageRouteKm, 1) + ' km'}</li>
          <li>Average cost: {data.averageCost === null ? '—' : '$' + requirementNumber(data.averageCost, 2)}</li>
          <li>Top candidate: {data.topCandidate ? data.topCandidate.materialTitle : 'No candidates'}</li>
        </ul></div>
      </div>
    </>}
  </section>;
}

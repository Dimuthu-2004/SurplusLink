import { useCallback, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { RequirementError, RequirementPagination, RequirementBadge, requirementDate, useRequirementResource } from '../../features/requirements/requirementUi';
import { managerWorkflowsApi, workflowStatuses, type ManagerWorkflowsApi, type WorkflowQuery } from '../../features/workflows/managerWorkflowsApi';

const defaults = { search: '', status: 'PENDING_APPROVAL', sortBy: 'startedAt' as WorkflowQuery['sortBy'], sortDir: 'desc' as WorkflowQuery['sortDir'], pageSize: 20 };
export function ManagerApprovalsPage({ api = managerWorkflowsApi }: { api?: ManagerWorkflowsApi }) {
  const [draft, setDraft] = useState(defaults); const [filters, setFilters] = useState(defaults); const [page, setPage] = useState(1);
  const query = useMemo<WorkflowQuery>(() => ({ ...filters, search: filters.search.trim(), page }), [filters, page]);
  const resource = useRequirementResource(useCallback(() => api.list(query), [api, query]));
  function apply(event: FormEvent) { event.preventDefault(); setFilters({ ...draft }); setPage(1); }
  return <div className="manager-page approval-page">
    <header className="page-heading"><div><p className="eyebrow">Manager workspace</p><h1>Pending approvals</h1><p className="muted">Review agent recommendations and make an accountable decision.</p></div></header>
    <section className="manager-panel" aria-label="Approval filters"><form className="filter-form" onSubmit={apply}>
      <label>Search stage or decision<input value={draft.search} maxLength={200} placeholder="Review, validation?" onChange={(event) => setDraft({ ...draft, search: event.target.value })} /></label>
      <label>Status<select value={draft.status} onChange={(event) => setDraft({ ...draft, status: event.target.value })}><option value="">All statuses</option>{workflowStatuses.map((status) => <option key={status} value={status}>{status.replaceAll('_', ' ')}</option>)}</select></label>
      <label>Sort by<select value={draft.sortBy} onChange={(event) => setDraft({ ...draft, sortBy: event.target.value as WorkflowQuery['sortBy'] })}><option value="startedAt">Started</option><option value="status">Status</option><option value="stage">Stage</option></select></label>
      <label>Direction<select value={draft.sortDir} onChange={(event) => setDraft({ ...draft, sortDir: event.target.value as WorkflowQuery['sortDir'] })}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label>
      <label>Items per page<select value={draft.pageSize} onChange={(event) => setDraft({ ...draft, pageSize: Number(event.target.value) })}>{[10, 20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}</select></label>
      <button className="button button-primary" type="submit">Apply filters</button><button className="button button-secondary" type="button" onClick={() => { setDraft(defaults); setFilters(defaults); setPage(1); }}>Reset filters</button>
    </form></section>
    <section className="manager-panel" aria-labelledby="approval-results"><div className="section-heading"><h2 id="approval-results">Workflows</h2>{resource.data && <span role="status">{resource.data.total} workflow{resource.data.total === 1 ? '' : 's'}</span>}</div>
      {resource.loading && <p role="status">Loading approvals?</p>}{resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
      {resource.data && (resource.data.items.length === 0 ? <p className="empty-state">No workflows match these filters.</p> : <><div className="table-scroll" role="region" tabIndex={0} aria-label="Approval workflow table"><table><caption className="sr-only">Agent workflows awaiting manager review</caption><thead><tr><th scope="col">Workflow</th><th scope="col">Status</th><th scope="col">Stage</th><th scope="col">Retries</th><th scope="col">Started</th><th scope="col">Review</th></tr></thead><tbody>{resource.data.items.map((item) => <tr key={item.id}><td><span title={item.id}>{item.id.slice(0, 8)}</span><small className="requirement-id">Match {item.materialMatchId?.slice(0, 8) ?? 'not linked'}</small></td><td><RequirementBadge status={item.status} /></td><td>{item.currentStage}</td><td>{item.retryCount}</td><td>{requirementDate(item.startedAtUtc)}</td><td><Link className="text-button" to={'/app/manager/workflows/' + item.id} aria-label={'Review workflow ' + item.id}>Review</Link></td></tr>)}</tbody></table></div><RequirementPagination page={resource.data.page} total={resource.data.total} pageSize={resource.data.pageSize} onPage={setPage} /></>)}</section>
  </div>;
}

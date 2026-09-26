import { ApprovalRequest } from './WorkflowSummary';
import { useCallback, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { RequirementError, RequirementPagination, RequirementBadge, requirementDate, useRequirementResource } from '../../features/requirements/requirementUi';
import { managerWorkflowsApi, workflowStatuses, type ManagerWorkflowsApi, type WorkflowQuery } from '../../features/workflows/managerWorkflowsApi';
import { useLiveReload } from '../../hooks/useLiveResource';

const defaults = { search: '', status: 'PENDING_APPROVAL', sortBy: 'startedAt' as WorkflowQuery['sortBy'], sortDir: 'desc' as WorkflowQuery['sortDir'], pageSize: 20 };
export function ManagerApprovalsPage({ api = managerWorkflowsApi }: { api?: ManagerWorkflowsApi }) {
  const [draft, setDraft] = useState(defaults); const [filters, setFilters] = useState(defaults); const [page, setPage] = useState(1);
  const query = useMemo<WorkflowQuery>(() => ({ ...filters, search: filters.search.trim(), page }), [filters, page]);
  const resource = useRequirementResource(useCallback(() => api.list(query), [api, query]));
  useLiveReload(resource.reload, resource.loading);
  function apply(event: FormEvent) { event.preventDefault(); setFilters({ ...draft }); setPage(1); }
  return <div className="manager-page approval-page">
    <header className="page-heading"><div><p className="eyebrow">Manager workspace</p><h1>Pending approvals</h1><p className="muted">Review agent recommendations and make an accountable decision.</p></div></header>
    <section className="manager-panel" aria-label="Approval filters"><form className="filter-form" onSubmit={apply}>
      <label>Search stage or decision<input value={draft.search} maxLength={200} placeholder="Review, validation..." onChange={(event) => setDraft({ ...draft, search: event.target.value })} /></label>
      <label>Status<select value={draft.status} onChange={(event) => setDraft({ ...draft, status: event.target.value })}><option value="">All statuses</option>{workflowStatuses.map((status) => <option key={status} value={status}>{status.replaceAll('_', ' ')}</option>)}</select></label>
      <label>Sort by<select value={draft.sortBy} onChange={(event) => setDraft({ ...draft, sortBy: event.target.value as WorkflowQuery['sortBy'] })}><option value="startedAt">Started</option><option value="status">Status</option><option value="stage">Stage</option></select></label>
      <label>Direction<select value={draft.sortDir} onChange={(event) => setDraft({ ...draft, sortDir: event.target.value as WorkflowQuery['sortDir'] })}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label>
      <label>Items per page<select value={draft.pageSize} onChange={(event) => setDraft({ ...draft, pageSize: Number(event.target.value) })}>{[10, 20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}</select></label>
      <button className="button button-primary" type="submit">Apply filters</button><button className="button button-secondary" type="button" onClick={() => { setDraft(defaults); setFilters(defaults); setPage(1); }}>Reset filters</button>
    </form></section>
    <section className="manager-panel" aria-labelledby="approval-results"><div className="section-heading"><h2 id="approval-results">Approval groups</h2>{resource.data && <span role="status">{resource.data.total} group{resource.data.total === 1 ? '' : 's'}</span>}</div>
      {resource.loading && <p role="status">Loading approvals...</p>}{resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
      {resource.data && (resource.data.items.length === 0 ? <p className="empty-state">No workflows match these filters.</p> : <><div className="table-scroll" role="region" tabIndex={0} aria-label="Approval group table"><table><caption className="sr-only">Buyer-confirmed approval groups awaiting manager review</caption><thead><tr><th scope="col">Requirement / buyer</th><th scope="col">Requested / selected</th><th scope="col">Fulfillment</th><th scope="col">Sellers / value</th><th scope="col">Status</th><th scope="col">View details</th></tr></thead><tbody>{resource.data.items.map((item) => { const group = item.approvalGroup; return <tr key={item.id}><td>{group ? <><strong>{group.requirementTitle}</strong><small className="requirement-id">Buyer: {group.buyerName}</small></> : <ApprovalRequest id={item.materialRequestId} />}<small className="requirement-id" title={item.id}>Group reference: {item.id.slice(0, 8)}</small></td><td>{group ? <>{group.requestedQuantity} {group.unit}<small className="requirement-id">Selected: {group.selectedQuantity} {group.unit} · Remaining: {group.remainingQuantity} {group.unit}</small></> : 'Not available'}</td><td>{group ? group.fulfillmentStatus === 'FULL' ? 'Full' : 'Partial' : item.currentStage}</td><td>{group ? <>{group.sellerCount} seller{group.sellerCount === 1 ? '' : 's'}<small className="requirement-id">LKR {group.totalValue.toLocaleString()}</small></> : 'Not available'}</td><td><RequirementBadge status={item.status} /></td><td><Link className="text-button" to={'/app/manager/workflows/' + item.id} aria-label={'View approval group ' + item.id}>View details</Link></td></tr>; })}</tbody></table></div><RequirementPagination page={resource.data.page} total={resource.data.total} pageSize={resource.data.pageSize} onPage={setPage} /></>)}</section>
  </div>;
}

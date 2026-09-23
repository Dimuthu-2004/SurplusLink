import { useCallback, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { managerRequirementsApi, requirementStatuses, type ManagerRequirementsApi, type RequirementQuery } from '../../features/requirements/managerRequirementsApi';
import { RequestAnalyticsWidget } from '../../features/requirements/RequestAnalyticsWidget';
import { RequirementBadge, RequirementError, RequirementPagination, requirementDate, requirementNumber, statusLabel, useRequirementResource } from '../../features/requirements/requirementUi';

const defaults = { search: '', categoryId: '', status: '', deadlineFrom: '', deadlineTo: '',
  sort: 'createdAt' as RequirementQuery['sort'], sortDir: 'desc' as RequirementQuery['sortDir'], pageSize: 20 };
export function ManagerRequirementsPage({ api = managerRequirementsApi }: { api?: ManagerRequirementsApi }) {
  const [draft, setDraft] = useState(defaults);
  const [filters, setFilters] = useState(defaults);
  const [page, setPage] = useState(1);
  const [validation, setValidation] = useState('');
  const query = useMemo<RequirementQuery>(() => ({ ...filters, search: filters.search.trim(), page,
    deadlineFrom: dateBoundary(filters.deadlineFrom, false), deadlineTo: dateBoundary(filters.deadlineTo, true),
  }), [filters, page]);
  const list = useRequirementResource(useCallback(() => api.list(query), [api, query]));
  const categories = useRequirementResource(useCallback(() => api.categories(), [api]));
  const names = new Map(categories.data?.map((category) => [category.id, category.name]));
  function apply(event: FormEvent) {
    event.preventDefault();
    if (draft.deadlineFrom && draft.deadlineTo && draft.deadlineFrom > draft.deadlineTo) {
      setValidation('Deadline from must be on or before deadline to.'); return;
    }
    setValidation(''); setPage(1); setFilters({ ...draft });
  }
  return <div className="manager-page requirement-page">
    <header className="page-heading"><div><p className="eyebrow">Manager workspace</p><h1>Buyer Requirements</h1>
      <p className="muted">Monitor buyer demand, matching status and request history.</p></div></header>
    <RequestAnalyticsWidget api={api} />
    <section className="manager-panel" aria-label="Requirement filters">
      {categories.error && <RequirementError message={'Unable to load categories. ' + categories.error} retry={categories.reload} label="Retry categories" />}
      <form className="filter-form" onSubmit={apply}>
        <label>Search category or notes<input maxLength={200} placeholder="Search buyer demand" value={draft.search}
          onChange={(event) => setDraft({ ...draft, search: event.target.value })} /></label>
        <label>Category<select value={draft.categoryId} disabled={categories.loading || !!categories.error}
          onChange={(event) => setDraft({ ...draft, categoryId: event.target.value })}>
          <option value="">{categories.loading ? 'Loading categories…' : 'All categories'}</option>
          {categories.data?.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
        </select></label>
        <label>Status<select value={draft.status} onChange={(event) => setDraft({ ...draft, status: event.target.value })}>
          <option value="">All statuses</option>{requirementStatuses.map((status) => <option key={status} value={status}>{statusLabel(status)}</option>)}
        </select></label>
        <label>Deadline from<input type="date" value={draft.deadlineFrom}
          onChange={(event) => setDraft({ ...draft, deadlineFrom: event.target.value })} /></label>
        <label>Deadline to<input type="date" value={draft.deadlineTo}
          onChange={(event) => setDraft({ ...draft, deadlineTo: event.target.value })} /></label>
        <label>Sort by<select value={draft.sort} onChange={(event) => setDraft({ ...draft, sort: event.target.value as RequirementQuery['sort'] })}>
          <option value="createdAt">Created date</option><option value="deadline">Deadline</option><option value="budget">Budget</option>
        </select></label>
        <label>Direction<select value={draft.sortDir} onChange={(event) => setDraft({ ...draft, sortDir: event.target.value as RequirementQuery['sortDir'] })}>
          <option value="desc">Descending</option><option value="asc">Ascending</option>
        </select></label>
        <label>Items per page<select value={draft.pageSize} onChange={(event) => setDraft({ ...draft, pageSize: Number(event.target.value) })}>
          {[10, 20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}
        </select></label>
        <button type="submit" className="button button-primary">Apply filters</button>
        <button type="button" className="button button-secondary" onClick={() => {
          setDraft(defaults); setFilters({ ...defaults }); setPage(1); setValidation('');
        }}>Reset filters</button>
      </form>
      {validation && <p className="field-error" role="alert">{validation}</p>}
      <p className="muted">Deadline dates include the whole selected day in your local time.</p>
    </section>
    <section className="manager-panel" aria-labelledby="requirements-results">
      <div className="section-heading"><h2 id="requirements-results">Requirements</h2>
        {list.data && <span role="status">{list.data.total} matching request{list.data.total === 1 ? '' : 's'}</span>}</div>
      {list.loading && <p role="status">Loading requirements…</p>}
      {list.error && <RequirementError message={list.error} retry={list.reload} />}
      {list.data && <>
        {list.data.items.length === 0 ? <p className="empty-state">No requirements match these filters.</p> :
          <div className="table-scroll" tabIndex={0} role="region" aria-label="Buyer requirements table">
            <table><caption className="sr-only">Buyer requirements with quantity, budget, deadline and current status</caption>
              <thead><tr><th scope="col">Request / Buyer</th><th scope="col">Category</th><th scope="col">Status</th>
                <th scope="col">Quantity</th><th scope="col">Maximum budget</th><th scope="col">Deadline</th><th scope="col">Created</th><th scope="col">Details</th></tr></thead>
              <tbody>{list.data.items.map((row) => <tr key={row.id}>
                <td><strong>{names.get(row.categoryId) ?? 'Material'} request</strong><small className="requirement-id" title={row.id}>Reference: {row.id.slice(0, 8)}</small><small className="requirement-id" title={row.buyerId}>Buyer {row.buyerId.slice(0, 8)}</small></td>
                <td>{names.get(row.categoryId) ?? 'Category unavailable'}</td><td><RequirementBadge status={row.status} /></td>
                <td>{requirementNumber(row.requiredQuantity)} {row.unit}</td><td>{requirementNumber(row.maximumBudget, 2)}</td>
                <td>{requirementDate(row.deadline)}</td><td>{requirementDate(row.createdAt)}</td>
                <td><Link className="text-button" aria-label={'View requirement ' + row.id} to={'/app/manager/requirements/' + row.id}>View</Link></td>
              </tr>)}</tbody>
            </table>
          </div>}
        <RequirementPagination page={list.data.page} total={list.data.total} pageSize={list.data.pageSize} onPage={setPage} />
      </>}
    </section>
  </div>;
}
export function dateBoundary(value: string, end: boolean): string | undefined {
  if (!value) return undefined;
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(year, month - 1, day, end ? 23 : 0, end ? 59 : 0, end ? 59 : 0, end ? 999 : 0);
  return date.toISOString();
}

import { useCallback, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { transactionsApi, offerStatuses, type Offer, type TransactionQuery, type TransactionsApi } from '../features/transactions/transactionsApi';
import { RequirementBadge, RequirementError, RequirementPagination, requirementDate, requirementNumber, statusLabel, useRequirementResource } from '../features/requirements/requirementUi';

const defaults: TransactionQuery = { status: '', sortBy: 'createdAt', sortDir: 'desc', page: 1, pageSize: 20 };
export function MyOffersPage({ api = transactionsApi }: { api?: TransactionsApi }) {
  const { user } = useAuth();
  const [draft, setDraft] = useState(defaults);
  const [filters, setFilters] = useState(defaults);
  const [page, setPage] = useState(1);
  const query = useMemo(() => ({ ...filters, page, userId: user?.id }), [filters, page, user?.id]);
  const resource = useRequirementResource(useCallback(() => api.offers(query), [api, query]));
  function apply(event: FormEvent) { event.preventDefault(); setPage(1); setFilters({ ...draft }); }
  return <div className="manager-page">
    <header className="page-heading"><div><p className="eyebrow">Marketplace activity</p><h1>My Offers</h1>
      <p className="muted">Offers where you participate as a buyer or seller.</p></div></header>
    <section className="manager-panel" aria-label="Offer filters"><form className="filter-form" onSubmit={apply}>
      <label>Status<select value={draft.status} onChange={e => setDraft({ ...draft, status: e.target.value })}><option value="">All statuses</option>{offerStatuses.map(s => <option key={s} value={s}>{statusLabel(s)}</option>)}</select></label>
      <label>Created from<input type="date" value={draft.createdFrom?.slice(0, 10) ?? ''} onChange={e => setDraft({ ...draft, createdFrom: e.target.value || undefined })} /></label>
      <label>Created to<input type="date" value={draft.createdTo?.slice(0, 10) ?? ''} onChange={e => setDraft({ ...draft, createdTo: e.target.value || undefined })} /></label>
      <label>Sort by<select value={draft.sortBy} onChange={e => setDraft({ ...draft, sortBy: e.target.value as TransactionQuery['sortBy'] })}><option value="createdAt">Created date</option><option value="value">Value</option><option value="status">Status</option></select></label>
      <label>Direction<select value={draft.sortDir} onChange={e => setDraft({ ...draft, sortDir: e.target.value as TransactionQuery['sortDir'] })}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label>
      <button className="button button-primary" type="submit">Apply filters</button>
    </form></section>
    <section className="manager-panel" aria-labelledby="offers-heading"><div className="section-heading"><h2 id="offers-heading">Offers</h2>{resource.data && <span className="muted">{resource.data.total} matching</span>}</div>
      {resource.loading && <p role="status">Loading offers...</p>}{resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
      {resource.data && (resource.data.items.length === 0 ? <p className="empty-state">No offers match these filters.</p> : <div className="table-scroll"><table><thead><tr><th>Offer</th><th>Context</th><th>Quantity</th><th>Value</th><th>Status</th><th>Created</th><th /></tr></thead><tbody>{resource.data.items.map((offer: Offer) => <tr key={offer.id}><td title={offer.id}>{offer.id.slice(0, 8)}</td><td>{offer.buyerId === user?.id ? 'Buyer participation' : 'Seller participation'}</td><td>{requirementNumber(offer.quantity)}</td><td>{requirementNumber(offer.totalValue, 2)}</td><td><RequirementBadge status={offer.status} /></td><td>{requirementDate(offer.createdAt)}</td><td><Link className="text-button" to={`/app/offers/${offer.id}`}>Details</Link></td></tr>)}</tbody></table></div>)}
      {resource.data && <RequirementPagination page={resource.data.page} total={resource.data.total} pageSize={resource.data.pageSize} onPage={setPage} />}
    </section>
  </div>;
}

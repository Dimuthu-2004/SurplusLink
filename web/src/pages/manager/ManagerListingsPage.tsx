import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { InventoryAnalyticsWidget } from '../../features/materials/InventoryAnalyticsWidget';
import {
  managerMaterialsApi,
  type ListingSearch,
  type ManagerMaterialsApi,
  type MaterialCategory,
  type PagedListings,
} from '../../features/materials/managerMaterialsApi';

const defaults: Omit<ListingSearch, 'page' | 'pageSize'> = {
  search: '',
  category: '',
  status: '',
  condition: '',
  minPrice: undefined,
  maxPrice: undefined,
  sortBy: 'createdAt',
  sortDir: 'desc',
};

export function ManagerListingsPage({
  api = managerMaterialsApi,
}: {
  api?: ManagerMaterialsApi;
}) {
  const [draft, setDraft] = useState(defaults);
  const [filters, setFilters] = useState(defaults);
  const [page, setPage] = useState(1);
  const [categories, setCategories] = useState<MaterialCategory[]>([]);
  const [result, setResult] = useState<PagedListings | null>(null);
  const [error, setError] = useState<string | null>(null);

  const query = useMemo<ListingSearch>(
    () => ({ ...filters, page, pageSize: 20 }),
    [filters, page],
  );

  useEffect(() => {
    let active = true;
    void api.getCategories().then(
      (items) => active && setCategories(items),
      () => active && setCategories([]),
    );
    return () => { active = false; };
  }, [api]);

  useEffect(() => {
    let active = true;
    setError(null);
    setResult(null);
    void api.listListings(query).then(
      (items) => active && setResult(items),
      (reason: unknown) => active && setError(messageFor(reason)),
    );
    return () => { active = false; };
  }, [api, query]);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPage(1);
    setFilters({
      ...draft,
      search: draft.search?.trim(),
      category: draft.category || undefined,
      status: draft.status || undefined,
      condition: draft.condition || undefined,
    });
  }

  return (
    <div className="manager-page">
      <section className="page-heading">
        <div>
          <p className="eyebrow">Manager workspace</p>
          <h1>Material listings</h1>
          <p className="muted">Review, verify, and monitor every submitted listing.</p>
        </div>
        <Link className="button button-secondary" to="/app/manager/categories">Manage categories</Link>
      </section>

      <InventoryAnalyticsWidget api={api} />

      <section className="manager-panel">
        <form className="filter-form" onSubmit={applyFilters}>
          <label>
            Search
            <input
              aria-label="Search"
              value={draft.search ?? ''}
              maxLength={200}
              placeholder="Title, description, or category"
              onChange={(event) => setDraft({ ...draft, search: event.target.value })}
            />
          </label>
          <label>
            Category
            <select
              aria-label="Category"
              value={draft.category ?? ''}
              onChange={(event) => setDraft({ ...draft, category: event.target.value })}
            >
              <option value="">All categories</option>
              {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
            </select>
          </label>
          <label>
            Status
            <select
              aria-label="Status"
              value={draft.status ?? ''}
              onChange={(event) => setDraft({ ...draft, status: event.target.value })}
            >
              <option value="">All statuses</option>
              <option value="PENDING_VERIFICATION">Pending verification</option>
              <option value="ACTIVE">Active</option>
              <option value="REJECTED">Rejected</option>
              <option value="DRAFT">Draft</option>
            </select>
          </label>
          <label>
            Condition
            <select
              aria-label="Condition"
              value={draft.condition ?? ''}
              onChange={(event) => setDraft({ ...draft, condition: event.target.value })}
            >
              <option value="">All conditions</option>
              {['NEW', 'EXCELLENT', 'GOOD', 'FAIR', 'POOR'].map((condition) => <option key={condition}>{condition}</option>)}
            </select>
          </label>
          <label>
            Minimum price
            <input
              aria-label="Minimum price"
              type="number"
              min="0"
              step="0.01"
              value={draft.minPrice ?? ''}
              onChange={(event) => setDraft({ ...draft, minPrice: numberOrUndefined(event.target.value) })}
            />
          </label>
          <label>
            Maximum price
            <input
              aria-label="Maximum price"
              type="number"
              min="0"
              step="0.01"
              value={draft.maxPrice ?? ''}
              onChange={(event) => setDraft({ ...draft, maxPrice: numberOrUndefined(event.target.value) })}
            />
          </label>
          <label>
            Sort by
            <select
              aria-label="Sort by"
              value={draft.sortBy}
              onChange={(event) => setDraft({ ...draft, sortBy: event.target.value as ListingSearch['sortBy'] })}
            >
              <option value="createdAt">Created date</option>
              <option value="availableUntil">Available until</option>
              <option value="unitPrice">Unit price</option>
              <option value="quantity">Quantity</option>
            </select>
          </label>
          <label>
            Direction
            <select
              aria-label="Direction"
              value={draft.sortDir}
              onChange={(event) => setDraft({ ...draft, sortDir: event.target.value as ListingSearch['sortDir'] })}
            >
              <option value="desc">Descending</option>
              <option value="asc">Ascending</option>
            </select>
          </label>
          <button className="button button-primary" type="submit">Apply filters</button>
        </form>
      </section>

      <section className="manager-panel" aria-labelledby="listings-heading">
        <div className="section-heading">
          <h2 id="listings-heading">Results</h2>
          {result && <span className="muted">{result.totalCount} listing{result.totalCount === 1 ? '' : 's'}</span>}
        </div>
        {error ? (
          <div className="error-message" role="alert">
            {error} <button className="text-button" type="button" onClick={() => setFilters({ ...filters })}>Retry</button>
          </div>
        ) : !result ? (
          <p aria-live="polite">Loading listings...</p>
        ) : result.items.length === 0 ? (
          <p className="empty-state">No listings match the current filters.</p>
        ) : (
          <div className="table-scroll">
            <table>
              <thead>
                <tr><th>Title</th><th>Category</th><th>Status</th><th>Quantity</th><th>Unit price</th><th>Available until</th><th /></tr>
              </thead>
              <tbody>
                {result.items.map((listing) => (
                  <tr key={listing.id}>
                    <td>{listing.title}</td>
                    <td>{listing.categoryName}</td>
                    <td><span className="status-badge">{listing.status.replaceAll('_', ' ')}</span></td>
                    <td>{listing.quantity - listing.reservedQuantity} {listing.unit}</td>
                    <td>{formatPrice(listing.unitPrice)}</td>
                    <td>{formatDate(listing.availableUntil)}</td>
                    <td><Link className="text-button" to={`/app/manager/materials/${listing.id}`}>Review</Link></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {result && result.totalPages > 1 && (
          <div className="pagination" aria-label="Pagination">
            <button className="button button-secondary" type="button" disabled={result.page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span>Page {result.page} of {result.totalPages}</span>
            <button className="button button-secondary" type="button" disabled={result.page >= result.totalPages} onClick={() => setPage(page + 1)}>Next</button>
          </div>
        )}
      </section>
    </div>
  );
}

function numberOrUndefined(value: string): number | undefined {
  return value === '' ? undefined : Number(value);
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
}

function messageFor(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Unable to load material listings.';
}

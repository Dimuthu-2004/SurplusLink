import { useCallback, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { MatchAnalyticsWidget } from '../../features/matches/MatchAnalyticsWidget';
import { managerMatchesApi, type ManagerMatchesApi, type MatchComparisonQuery, type MatchCandidate } from '../../features/matches/managerMatchesApi';
import { RequirementBadge, RequirementError, RequirementPagination, requirementDate, requirementNumber, useRequirementResource } from '../../features/requirements/requirementUi';

const defaultQuery = {
  status: 'ALL' as MatchComparisonQuery['status'],
  sort: 'score' as MatchComparisonQuery['sort'],
  sortDir: 'desc' as MatchComparisonQuery['sortDir'],
  page: 1,
  pageSize: 20,
};

export function ManagerMatchComparisonPage({ api = managerMatchesApi }: { api?: ManagerMatchesApi }) {
  const { requirementId = '' } = useParams();
  const [status, setStatus] = useState<'ALL' | 'VALID' | 'REJECTED'>('ALL');
  const [sort, setSort] = useState<MatchComparisonQuery['sort']>('score');
  const [sortDir, setSortDir] = useState<MatchComparisonQuery['sortDir']>('desc');
  const [page, setPage] = useState(1);

  const requirement = useRequirementResource(useCallback(() => api.requirement(requirementId), [api, requirementId]));
  const query = useMemo<MatchComparisonQuery>(() => ({
    status,
    sort,
    sortDir,
    page,
    pageSize: 20,
  }), [page, sort, sortDir, status]);
  const list = useRequirementResource(useCallback(() => api.list(requirementId, query), [api, requirementId, query]));

  const row = requirement.data;
  const done = (list.data?.items ?? []).length > 0;

  return <div className="manager-page requirement-page">
    <Link className="back-link" to={'/app/manager/requirements/' + encodeURIComponent(requirementId)}>
      View requirement
    </Link>
    <header className="page-heading">
      <div>
        <p className="eyebrow">Manager review</p>
        <h1>Match comparison</h1>
      </div>
      <button type="button" className="button button-secondary" disabled={list.loading} onClick={list.reload}>Refresh matches</button>
    </header>

    {requirement.loading && <p role="status">Loading requirement context…</p>}
    {requirement.error && <RequirementError message={requirement.error} retry={requirement.reload} />}
    {row && <section className="manager-panel">
      <div className="section-heading">
        <h2>Requirement context</h2>
        <RequirementBadge status={row.status} />
      </div>
      <dl className="detail-grid">
        <div><dt>Requirement ID</dt><dd>{row.id}</dd></div>
        <div><dt>Category</dt><dd>{row.categoryId}</dd></div>
        <div><dt>Required quantity</dt><dd>{requirementNumber(row.requiredQuantity)} {row.unit}</dd></div>
        <div><dt>Budget</dt><dd>${requirementNumber(row.maximumBudget, 2)}</dd></div>
        <div><dt>Deadline</dt><dd>{requirementDate(row.deadline)}</dd></div>
        <div><dt>Location</dt><dd>{row.latitude === null || row.longitude === null ? 'Not recorded' : `${row.latitude}, ${row.longitude}`}</dd></div>
      </dl>
      <p className="muted">{row.notes || 'No notes added.'}</p>
    </section>}

    <MatchAnalyticsWidget api={api} requirementId={requirementId} />

    <section className="manager-panel" aria-label="Candidate comparison filters">
      <div className="section-heading">
        <h2>Matched candidates</h2>
      </div>
      <div className="filter-row" role="group" aria-label="Candidate status filter">
        {(['ALL', 'VALID', 'REJECTED'] as const).map((option) => (
          <button key={option} type="button" className={status === option ? 'button button-primary' : 'button button-secondary'}
            onClick={() => { setStatus(option); setPage(1); }}>
            {option}
          </button>
        ))}
      </div>
      <div className="filter-form no-gap">
        <label>Sort by<select value={sort} onChange={(event) => setSort(event.target.value as MatchComparisonQuery['sort'])}>
          <option value="score">Score</option>
          <option value="distance">Distance</option>
          <option value="cost">Cost</option>
        </select></label>
        <label>Direction<select value={sortDir} onChange={(event) => setSortDir(event.target.value as MatchComparisonQuery['sortDir'])}>
          <option value="desc">Descending</option>
          <option value="asc">Ascending</option>
        </select></label>
      </div>
    </section>

    <section className="manager-panel" aria-labelledby="candidate-results-heading">
      <div className="section-heading"><h2 id="candidate-results-heading">Candidate table</h2>
        {list.data && <span role="status">{list.data.total} match{list.data.total === 1 ? '' : 'es'}</span>}
      </div>
      {list.loading && <p role="status">Loading candidate matches…</p>}
      {list.error && <RequirementError message={list.error} retry={list.reload} />}
      {list.data && <>
        {!done ? <p className="empty-state">No matches match these filters.</p> : <>
          <div className="table-scroll" tabIndex={0} role="region" aria-label="Match comparison table">
            <table>
              <thead>
                <tr>
                  <th scope="col">Candidate</th>
                  <th scope="col">Score</th>
                  <th scope="col">Route</th>
                  <th scope="col">Estimated cost</th>
                  <th scope="col">Status</th>
                  <th scope="col">Rejection reason</th>
                  <th scope="col">Links</th>
                </tr>
              </thead>
              <tbody>
                {list.data.items.map((candidate) => <MatchCandidateRow key={candidate.id} candidate={candidate} requirementId={requirementId} />)}
              </tbody>
            </table>
          </div>
          <RequirementPagination page={list.data.page} total={list.data.total} pageSize={list.data.pageSize} onPage={setPage} />
        </>}
      </>}
    </section>
  </div>;
}

function MatchCandidateRow({ candidate, requirementId }: { candidate: MatchCandidate; requirementId: string }) {
  return <tr>
    <td>
      <strong>{candidate.materialTitle}</strong><br />
      <small>{candidate.categoryName}</small>
    </td>
    <td>{candidate.score}</td>
    <td>{candidate.routeDistanceKm === null ? '—' : `${requirementNumber(candidate.routeDistanceKm, 1)} km`}</td>
    <td>{candidate.estimatedCost === null ? '—' : `$${requirementNumber(candidate.estimatedCost, 2)}`}</td>
    <td><span className="status-badge">{candidate.status}</span></td>
    <td>{candidate.rejectionReason || '—'}</td>
    <td>
      <Link className="text-button" to={'/app/manager/materials/' + encodeURIComponent(candidate.materialListingId)}>View material</Link>
      <br />
      <Link className="text-button" to={'/app/manager/requirements/' + encodeURIComponent(requirementId)}>View requirement</Link>
    </td>
  </tr>;
}

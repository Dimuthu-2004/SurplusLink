import { useCallback, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { MatchAnalyticsWidget } from '../../features/matches/MatchAnalyticsWidget';
import { managerMatchesApi, type ManagerMatchesApi, type MatchComparisonQuery, type MatchCandidate, type MatchStatus } from '../../features/matches/managerMatchesApi';
import { matchReason, matchCurrency, matchRoute } from '../../features/matches/matchFormatters';
import { RequirementBadge, RequirementError, RequirementPagination, requirementDate, requirementNumber, useRequirementResource } from '../../features/requirements/requirementUi';

export function ManagerMatchComparisonPage({ api = managerMatchesApi }: { api?: ManagerMatchesApi }) {
  const { requirementId } = useParams();
  if (!requirementId) return <section className="manager-panel">
    <h1>Match comparison</h1><p>Select a buyer requirement to compare its match candidates.</p>
    <Link to="/app/manager/requirements">Choose requirement</Link>
  </section>;
  return <RequirementMatches key={requirementId} api={api} requirementId={requirementId} />;
}

function RequirementMatches({ api, requirementId }: { api: ManagerMatchesApi; requirementId: string }) {
  const [matchStatus, setMatchStatus] = useState<MatchStatus | ''>('');
  const [revision, setRevision] = useState(0);
  const [status, setStatus] = useState<'ALL' | 'VALID' | 'REJECTED'>('ALL');
  const [sort, setSort] = useState<MatchComparisonQuery['sort']>('score');
  const [sortDir, setSortDir] = useState<MatchComparisonQuery['sortDir']>('desc');
  const [page, setPage] = useState(1);

  const requirement = useRequirementResource(useCallback(() => api.requirement(requirementId), [api, requirementId]));
  const query = useMemo<MatchComparisonQuery>(() => ({
    status,
    matchStatus: matchStatus || undefined,
    sort,
    sortDir,
    page,
    pageSize: 20,
  }), [page, sort, sortDir, status, matchStatus]);
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
      <button type="button" className="button button-secondary" disabled={list.loading} onClick={() => { list.reload(); requirement.reload(); setRevision(value => value + 1); }}>Refresh matches</button>
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
        <div><dt>Budget</dt><dd>{matchCurrency(row.maximumBudget)}</dd></div>
        <div><dt>Deadline</dt><dd>{requirementDate(row.deadline)}</dd></div>
        <div><dt>Location</dt><dd>{row.latitude === null || row.longitude === null ? 'Not recorded' : `${row.latitude}, ${row.longitude}`}</dd></div>
      </dl>
      <p className="muted">{row.notes || 'No notes added.'}</p>
    </section>}

    <MatchAnalyticsWidget key={revision} api={api} requirementId={requirementId} />

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
        <label>Match status<select value={matchStatus} onChange={event => { setMatchStatus(event.target.value as MatchStatus | ''); setPage(1); }}>
          <option value="">Any status</option>
          {(['GENERATED', 'RANKED', 'ROUTED', 'ROUTE_FAILED', 'REJECTED'] as const).map(value => <option key={value} value={value}>{value.replaceAll('_', ' ')}</option>)}
        </select></label>
        <label>Sort by<select value={sort} onChange={(event) => { setSort(event.target.value as MatchComparisonQuery['sort']); setPage(1); }}>
          <option value="score">Score</option>
          <option value="distance">Distance</option>
          <option value="cost">Cost</option>
        </select></label>
        <label>Direction<select value={sortDir} onChange={(event) => { setSortDir(event.target.value as MatchComparisonQuery['sortDir']); setPage(1); }}>
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
        {!done ? <p className="empty-state">{status === 'ALL' && !matchStatus ? 'No match candidates have been generated for this requirement yet.' : 'No candidates match these filters.'}</p> : <>
          <div className="table-scroll" tabIndex={0} role="region" aria-label="Match comparison table">
            <table>
              <thead>
                <tr>
                  <th scope="col">Candidate</th>
                  <th scope="col">Required quantity</th><th scope="col">Available quantity</th><th scope="col">Unit price</th>
                  <th scope="col">Score</th>
                  <th scope="col">Route</th>
                  <th scope="col">Transport cost (LKR)</th>
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
    <td>{candidate.quantity == null ? 'Not available' : `${requirementNumber(candidate.quantity)} ${candidate.unit ?? ''}`}</td>
    <td>{candidate.availableQuantity == null ? 'Not available' : `${requirementNumber(candidate.availableQuantity)} ${candidate.unit ?? ''}`}</td>
    <td>{matchCurrency(candidate.unitPrice)}</td>
    <td>{requirementNumber(candidate.score, 1)}%</td>
    <td>{matchRoute(candidate)}</td>
    <td>{matchCurrency(candidate.estimatedCost)}</td>
    <td><RequirementBadge status={candidate.status} /><br />{candidate.rejected || candidate.status === 'REJECTED' ? 'Rejected' : 'Not rejected'}</td>
    <td>{candidate.rejectionReason ? matchReason(candidate.rejectionReason) : 'None'}</td>
    <td>
      <Link className="text-button" to={'/app/manager/materials/' + encodeURIComponent(candidate.materialListingId)}>View material</Link>
      <br />
      <Link className="text-button" to={'/app/manager/requirements/' + encodeURIComponent(requirementId)}>View requirement</Link>
    </td>
  </tr>;
}

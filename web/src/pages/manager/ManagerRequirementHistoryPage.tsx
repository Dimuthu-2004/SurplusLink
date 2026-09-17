import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { managerRequirementsApi, type ManagerRequirementsApi } from '../../features/requirements/managerRequirementsApi';
import { RequirementError, RequirementPagination, requirementDate, statusLabel, useRequirementResource } from '../../features/requirements/requirementUi';

export function ManagerRequirementHistoryPage({ api = managerRequirementsApi }: { api?: ManagerRequirementsApi }) {
  const { requirementId = '' } = useParams();
  // Key the content by ID so navigating between request histories resets pagination.
  return <HistoryContent key={requirementId} id={requirementId} api={api} />;
}
function HistoryContent({ id, api }: { id: string; api: ManagerRequirementsApi }) {
  const [page, setPage] = useState(1);
  const resource = useRequirementResource(useCallback(() => api.history(id, page), [api, id, page]));
  return <div className="manager-page requirement-page">
    <Link className="back-link" to={'/app/manager/requirements/' + encodeURIComponent(id)}>Back to requirement</Link>
    <header className="page-heading"><div><p className="eyebrow">Audit trail</p><h1>Requirement history</h1>
      <p className="muted requirement-notes">Request {id} · Oldest first</p></div>
      <button type="button" className="button button-secondary" disabled={resource.loading} onClick={resource.reload}>Refresh history</button>
    </header>
    <section className="manager-panel">
      {resource.loading && <p role="status">Loading history…</p>}
      {resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
      {resource.data && <>
        <p>{resource.data.total} history events</p>
        {resource.data.items.length === 0 ? <p className="empty-state">No history events on this page.</p> :
          <ol className="history-list" start={(resource.data.page - 1) * resource.data.pageSize + 1}>
            {resource.data.items.map((event) => <li key={event.id}>
              <strong>{event.fromStatus && event.toStatus ? statusLabel(event.fromStatus) + ' → ' + statusLabel(event.toStatus) : statusLabel(event.action)}</strong>
              <time dateTime={event.createdAt}>{requirementDate(event.createdAt)}</time>
              <span>{event.actorUserId ? 'Account ' + event.actorUserId : 'System action'}</span>
            </li>)}
          </ol>}
        <RequirementPagination page={resource.data.page} pageSize={resource.data.pageSize} total={resource.data.total} onPage={setPage} />
      </>}
    </section>
  </div>;
}

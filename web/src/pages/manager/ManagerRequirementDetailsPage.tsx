import { useCallback } from 'react';
import { Link, useParams } from 'react-router-dom';
import { managerRequirementsApi, type ManagerRequirementsApi } from '../../features/requirements/managerRequirementsApi';
import { RequirementBadge, RequirementError, requirementDate, requirementNumber, useRequirementResource } from '../../features/requirements/requirementUi';

export function ManagerRequirementDetailsPage({ api = managerRequirementsApi, workflowOnly = false }: {
  api?: ManagerRequirementsApi; workflowOnly?: boolean;
}) {
  const { requirementId = '' } = useParams();
  const resource = useRequirementResource(useCallback(() => api.get(requirementId), [api, requirementId]));
  const categories = useRequirementResource(useCallback(() => api.categories(), [api]));
  const row = resource.data;
  const base = '/app/manager/requirements/' + encodeURIComponent(requirementId);
  return <div className="manager-page requirement-page">
    <Link className="back-link" to={workflowOnly ? base : '/app/manager/requirements'}>
      {workflowOnly ? 'Back to requirement' : 'Back to Buyer Requirements'}</Link>
    <header className="page-heading"><div><p className="eyebrow">Manager review</p>
      <h1>{workflowOnly ? 'Workflow status' : 'Requirement details'}</h1></div>
      <button type="button" className="button button-secondary" disabled={resource.loading} onClick={resource.reload}>Refresh status</button>
    </header>
    {resource.loading && <p role="status">Loading requirement…</p>}
    {resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
    {row && <section className="manager-panel">
      <div className="section-heading"><h2>{categories.data?.find((item) => item.id === row.categoryId)?.name ?? 'Buyer requirement'}</h2>
        <RequirementBadge status={row.status} /></div>
      {workflowOnly ? <>
        <p>{workflowDescription(row.status)}</p>
        <p className="muted">Last updated {requirementDate(row.updatedAt)}.</p>
        <h3>Workflow and recommendations</h3>
        {row.workflowId ? <><RequirementBadge status={row.workflowStatus ?? row.status} /><Link className="button button-secondary" to={'/app/manager/workflows/' + row.workflowId}>Inspect agent workflow</Link></> : <p>No workflow has been started for this requirement.</p>}
        <Link className="button button-secondary" to={base + '/matches'}>View match comparison</Link>
        {row.decisionNote && <p>Manager note: {row.decisionNote}</p>}
        <p>The status above reflects the latest saved requirement. Refresh to check for updates.</p>
      </> : <>
        {categories.error && <RequirementError message={'Category name unavailable. ' + categories.error} retry={categories.reload} label="Retry category name" />}
        <dl className="detail-grid">

          <div><dt>Required quantity</dt><dd>{requirementNumber(row.requiredQuantity)} {row.unit}</dd></div>
          <div><dt>Maximum budget</dt><dd>{requirementNumber(row.maximumBudget, 2)}</dd></div>
          <div><dt>Deadline (local time)</dt><dd>{requirementDate(row.deadline)}</dd></div>
          <div><dt>Delivery coordinates</dt><dd>{row.latitude === null || row.longitude === null ? 'Not recorded' : row.latitude + ', ' + row.longitude}</dd></div>
          <div><dt>Created</dt><dd>{requirementDate(row.createdAt)}</dd></div>
          <div><dt>Last updated</dt><dd>{requirementDate(row.updatedAt)}</dd></div>
        </dl>
        <small className="requirement-id" title={row.id}>Request reference: {row.id.slice(0, 8)}</small><small className="requirement-id" title={row.buyerId}>Buyer reference: {row.buyerId.slice(0, 8)}</small><h3>Notes</h3><p className="requirement-notes">{row.notes || 'No notes added.'}</p>
        <p className="muted">Manager access is read-only. Buyers manage their own requirements.</p>
      </>}
      <nav className="action-row" aria-label="Requirement views">
        {!workflowOnly && <Link className="button button-secondary" to={base + '/matches'}>View match comparison</Link>}
        {!workflowOnly && <Link className="button button-secondary" to={base + '/workflow'}>View workflow status</Link>}
        <Link className="button button-secondary" to={base + '/history'}>View history</Link>
      </nav>
    </section>}
  </div>;
}
function workflowDescription(status: string): string {
  const descriptions: Record<string, string> = {
    DRAFT: 'This requirement is a draft and has not been submitted.',
    OPEN: 'This requirement is open. Matching has not started.',
    MATCHING: 'Matching has started for this requirement.',
    MATCH_FOUND: 'A match has been found.',
    PENDING_APPROVAL: 'The match is waiting for approval.',
    APPROVED: 'The match has been approved.', REJECTED: 'The match was rejected.',
    COMPLETED: 'This requirement has been completed.', CANCELLED: 'This requirement has been cancelled.',
  };
  return descriptions[status] ?? 'The latest requirement status is shown above.';
}

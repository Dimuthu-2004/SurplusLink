import { apiClient } from '../../api/apiClient';
import { useCallback } from 'react';
import { Link } from 'react-router-dom';
import { managerRequirementsApi } from '../../features/requirements/managerRequirementsApi';
import { managerMaterialsApi } from '../../features/materials/managerMaterialsApi';
import { requirementNumber, statusLabel, useRequirementResource } from '../../features/requirements/requirementUi';
import type { ApprovalGroup, Workflow } from '../../features/workflows/managerWorkflowsApi';

function jsonObject(value: string): Record<string, unknown> {
  try { const parsed: unknown = JSON.parse(value); return object(parsed); } catch { return {}; }
}
function object(value: unknown): Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {};
}
function number(value: unknown, suffix = '') {
  return typeof value === 'number' && Number.isFinite(value) ? requirementNumber(value, 2) + suffix : 'Not available';
}
function messages(value: unknown) {
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string').map(statusLabel) : [];
}
function useRequest(id: string | null) {
  return useRequirementResource(useCallback(async () => {
    if (!id) return null;
    const [request, categories] = await Promise.all([managerRequirementsApi.get(id), managerRequirementsApi.categories().catch(() => [])]);
    return { ...request, category: categories.find(item => item.id === request.categoryId)?.name };
  }, [id]));
}

export function ApprovalRequest({ id }: { id: string | null }) {
  const { data, loading } = useRequest(id);
  return <><strong>{data ? `${data.category ?? 'Material'} request` : loading ? 'Loading request...' : 'Buyer request'}</strong>
    {data && <small className="requirement-id">{requirementNumber(data.requiredQuantity)} {data.unit} · Budget LKR {requirementNumber(data.maximumBudget, 2)}</small>}
    {!data && !loading && <small className="muted">{id ? 'Request details unavailable' : 'No linked request'}</small>}
  </>;
}

export function WorkflowSummary({ workflow }: { workflow: Workflow }) {
  const request = useRequest(workflow.materialRequestId);
  const output = jsonObject(workflow.outputJson);
  const recommendation = object(output.recommendation);
  const validation = jsonObject(workflow.validationJson);
  const recommendedId = typeof validation.recommendedMatchId === 'string' ? validation.recommendedMatchId : null;
  const current = useRequirementResource(useCallback(async () => {
    if (!recommendedId || validation.valid !== true || messages(validation.violations).length > 0 || !workflow.materialRequestId ||
      !['PENDING_APPROVAL', 'COMPLETED'].includes(workflow.status)) return null;
    const [matchResponse, requirement] = await Promise.all([
      apiClient.get<CurrentMatch>('/api/matches/' + encodeURIComponent(recommendedId)),
      managerRequirementsApi.get(workflow.materialRequestId),
    ]);
    const match = matchResponse.data;
    if (match.id !== recommendedId || match.requirementId !== requirement.id ||
      requirement.workflowId !== workflow.id || !['MATCH_FOUND', 'PENDING_APPROVAL'].includes(requirement.status) ||
      !match.valid || match.rejected || match.rejectionReason || match.status !== 'ROUTED' ||
      (workflow.materialMatchId && workflow.materialMatchId !== recommendedId) ||
      (recommendation.matchId && recommendation.matchId !== recommendedId) ||
      (output.selectedMatchId && output.selectedMatchId !== recommendedId)) return null;
    const listing = await managerMaterialsApi.getListing(match.listingId);
    if (listing.id !== match.listingId || (recommendation.listingId && recommendation.listingId !== listing.id) || listing.status !== 'ACTIVE' || !(Date.parse(listing.availableUntil) > Date.now()) ||
      !(Date.parse(requirement.deadline) > Date.now())) return null;
    return { match, listing };
  }, [recommendedId, workflow, validation.valid]));
  const selected = !current.loading && !current.error ? current.data : null;
  if (workflow.approvalGroup) return <ApprovalGroupDetails group={workflow.approvalGroup} />;
  const warnings = messages(validation.warnings ?? object(output.validation).warnings);
  const violations = messages(validation.violations ?? object(output.validation).violations);
  const row = request.data;
  const objective = jsonObject(workflow.inputJson).objective;
  const requestLabel = typeof objective === 'string' && objective.trim() ? objective : row ? `${row.category ?? 'Material'} request` : 'View buyer request';
  const matchReason = typeof selected?.match?.recommendationReason === 'string' ? selected.match.recommendationReason.trim() : '';
  const recReason = typeof recommendation.reason === 'string' ? recommendation.reason.trim() : typeof recommendation.recommendationReason === 'string' ? recommendation.recommendationReason.trim() : '';
  const wfReason = typeof workflow.recommendationReason === 'string' ? workflow.recommendationReason.trim() : '';
  const outReason = typeof output.recommendationReason === 'string' ? output.recommendationReason.trim() : '';
  const defaultReason = selected ? 'Highest deterministic final score among valid routed candidates; ties use condition, total estimated cost, distance, then listing ID.' : 'No current recommendation available.';
  const displayReason = matchReason || recReason || wfReason || outReason || defaultReason;
  return <>
    <dl className="detail-grid">
      <div><dt>Buyer / request</dt><dd>{workflow.materialRequestId ? <Link to={'/app/manager/requirements/' + workflow.materialRequestId}>{requestLabel}</Link> : 'No linked request'}</dd><small className="muted">Buyer name unavailable</small></div>
      <div><dt>Required quantity</dt><dd>{row ? `${requirementNumber(row.requiredQuantity)} ${row.unit}` : 'Not available'}</dd></div>
      <div><dt>Maximum budget (LKR)</dt><dd>{number(row?.maximumBudget)}</dd></div>
    </dl>
    {request.error && <p className="muted">Request summary unavailable. <button className="text-button" onClick={request.reload}>Retry request summary</button></p>}
    <section className="recommendation-card" aria-label="Current recommendation">
      <h3>Recommendation</h3>
      {current.loading ? <p role="status">Checking current recommendation...</p> : current.error ?
        <p className="error-message" role="alert">Recommendation could not be verified. <button className="text-button" onClick={current.reload}>Retry recommendation</button></p> : selected ? <>
        <p className="eyebrow">Current valid match</p>
        <dl className="detail-grid">
          <div><dt>Material</dt><dd><Link to={'/app/manager/materials/' + selected.listing.id}>{selected.listing.title}</Link></dd></div>
          <div><dt>Seller / business</dt><dd>{selected.listing.seller?.businessName || selected.listing.seller?.fullName || 'Seller details unavailable'}</dd></div>
          <div><dt>Score</dt><dd>{number(selected.match.score * 100, '%')}</dd></div>
          <div><dt>Route distance</dt><dd>{number(selected.match.distance, ' km')}</dd></div>
          <div><dt>Delivery duration</dt><dd>{number(selected.match.durationMinutes, ' min')}</dd></div>
          <div><dt>Unit price (LKR)</dt><dd>{number(selected.listing.unitPrice)} / {selected.listing.unit}</dd></div>
          <div><dt>Transport cost (LKR)</dt><dd>{number(selected.match.estimatedTransportCost)}</dd></div>
        </dl>
        <h4>Recommendation reason</h4><p>{displayReason}</p>
      </> : <p className="empty-state">No current valid recommendation is available.</p>}
    </section>
    <h3>Warnings and violations</h3>
    {warnings.length + violations.length > 0 ? <ul>{violations.map((text, index) => <li key={'v' + index}><strong>Violation:</strong> {text}</li>)}{warnings.map((text, index) => <li key={'w' + index}><strong>Warning:</strong> {text}</li>)}</ul> : <p>{Array.isArray(validation.warnings) && Array.isArray(validation.violations) ? 'None recorded.' : 'No warning or violation details recorded.'}</p>}
    {workflow.errorJson && <p className="error-message">Workflow error: {typeof jsonObject(workflow.errorJson).code === 'string' ? statusLabel(jsonObject(workflow.errorJson).code as string) : 'See Technical details.'}</p>}
  </>;
}

function ApprovalGroupDetails({ group }: { group: ApprovalGroup }) {
  const full = group.fulfillmentStatus === 'FULL';
  return <>
    <dl className="detail-grid">
      <div><dt>Buyer</dt><dd>{group.buyerName}</dd></div>
      <div><dt>Requirement</dt><dd>{group.requirementTitle}</dd></div>
      <div><dt>Total requested</dt><dd>{requirementNumber(group.requestedQuantity)} {group.unit}</dd></div>
      <div><dt>Total selected</dt><dd>{requirementNumber(group.selectedQuantity)} {group.unit}</dd></div>
      <div><dt>Remaining</dt><dd>{requirementNumber(group.remainingQuantity)} {group.unit}</dd></div>
      <div><dt>Fulfillment</dt><dd>{full ? 'Full' : 'Partial fulfillment'}</dd></div>
      <div><dt>Total deal value</dt><dd>LKR {requirementNumber(group.totalValue, 2)}</dd></div>
    </dl>
    {!full && <p className="decision-notice">Partial fulfillment: {requirementNumber(group.remainingQuantity)} {group.unit} remains unfulfilled.</p>}
    <section className="recommendation-card" aria-label="Seller allocation breakdown">
      <h3>Seller allocations ({group.sellerCount})</h3>
      <div className="table-scroll" role="region" tabIndex={0} aria-label="Seller allocations">
        <table><thead><tr><th>Seller / listing</th><th>Allocated / available</th><th>Price / value</th><th>AI / logistics</th><th>Status</th></tr></thead><tbody>
          {group.allocations.map((allocation) => <tr key={allocation.transactionId}>
            <td><strong>{allocation.sellerBusinessName || allocation.sellerName}</strong><small className="requirement-id">{allocation.listingTitle}</small></td>
            <td>{requirementNumber(allocation.allocatedQuantity)} {allocation.unit}<small className="requirement-id">Available: {requirementNumber(allocation.availableQuantity)} {allocation.unit}</small></td>
            <td>LKR {requirementNumber(allocation.unitPrice, 2)} / {allocation.unit}<small className="requirement-id">Value: LKR {requirementNumber(allocation.materialValue, 2)}</small></td>
            <td>Score: {allocation.score === null ? 'Not available' : `${requirementNumber(allocation.score * 100, 1)}%`}<small className="requirement-id">Distance: {allocation.distance === null ? 'Not available' : `${requirementNumber(allocation.distance, 2)} km`} · Transport: {allocation.transportCost === null ? 'Not available' : `LKR ${requirementNumber(allocation.transportCost, 2)}`}</small></td>
            <td>{allocation.status}</td>
          </tr>)}
        </tbody></table>
      </div>
    </section>
  </>;
}

interface CurrentMatch {
  id: string; requirementId: string; listingId: string; valid: boolean; rejected: boolean;
  rejectionReason: string | null; status: string; score: number; distance: number | null;
  durationMinutes: number | null; estimatedTransportCost: number | null; recommendationReason?: string | null;
}

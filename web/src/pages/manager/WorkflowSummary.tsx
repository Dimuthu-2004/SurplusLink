import { useCallback } from 'react';
import { Link } from 'react-router-dom';
import { managerRequirementsApi } from '../../features/requirements/managerRequirementsApi';
import { managerMaterialsApi } from '../../features/materials/managerMaterialsApi';
import { requirementNumber, statusLabel, useRequirementResource } from '../../features/requirements/requirementUi';
import type { Workflow } from '../../features/workflows/managerWorkflowsApi';

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
  const listingId = typeof recommendation.listingId === 'string' ? recommendation.listingId : null;
  const material = useRequirementResource(useCallback(() => listingId ? managerMaterialsApi.getListing(listingId) : Promise.resolve(null), [listingId]));
  const warnings = messages(validation.warnings ?? object(output.validation).warnings);
  const violations = messages(validation.violations ?? object(output.validation).violations);
  const row = request.data;
  const objective = jsonObject(workflow.inputJson).objective;
  const requestLabel = typeof objective === 'string' && objective.trim() ? objective : row ? `${row.category ?? 'Material'} request` : 'View buyer request';
  return <>
    <dl className="detail-grid">
      <div><dt>Material</dt><dd>{material.data ? <Link to={'/app/manager/materials/' + material.data.id}>{material.data.title}</Link> : material.loading ? 'Loading material...' : 'Material details unavailable'}</dd></div>
      <div><dt>Buyer / request</dt><dd>{workflow.materialRequestId ? <Link to={'/app/manager/requirements/' + workflow.materialRequestId}>{requestLabel}</Link> : 'No linked request'}</dd><small className="muted">Buyer name unavailable</small></div>
      <div><dt>Required quantity</dt><dd>{row ? `${requirementNumber(row.requiredQuantity)} ${row.unit}` : 'Not available'}</dd></div>
      <div><dt>Maximum budget (LKR)</dt><dd>{number(row?.maximumBudget)}</dd></div>
      <div><dt>Route distance</dt><dd>{number(recommendation.distanceKm, ' km')}</dd></div>
      <div><dt>Transport cost (LKR)</dt><dd>{number(recommendation.transportCost)}</dd></div>
    </dl>
    {request.error && <p className="muted">Request summary unavailable. <button className="text-button" onClick={request.reload}>Retry request summary</button></p>}
    {material.error && <p className="muted">Material name unavailable. <button className="text-button" onClick={material.reload}>Retry material name</button></p>}
    <h3>Recommendation</h3>
    <p>{typeof output.recommendation === 'string' ? output.recommendation : listingId ? 'Recommended material selected for this request.' : 'No recommendation recorded.'}</p>
    <h3>Warnings and violations</h3>
    {warnings.length + violations.length > 0 ? <ul>{violations.map((text, index) => <li key={'v' + index}><strong>Violation:</strong> {text}</li>)}{warnings.map((text, index) => <li key={'w' + index}><strong>Warning:</strong> {text}</li>)}</ul> : <p>{Array.isArray(validation.warnings) && Array.isArray(validation.violations) ? 'None recorded.' : 'No warning or violation details recorded.'}</p>}
    {workflow.errorJson && <p className="error-message">Workflow error: {typeof jsonObject(workflow.errorJson).code === 'string' ? statusLabel(jsonObject(workflow.errorJson).code as string) : 'See Technical details.'}</p>}
  </>;
}

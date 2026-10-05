import type { ReactNode } from 'react';
import { RequirementBadge, requirementDate, requirementNumber, statusLabel } from '../../features/requirements/requirementUi';
import { formatLkr } from '../../utils/currency';
import type { AgentStep, ToolCall, Workflow } from '../../features/workflows/managerWorkflowsApi';

type Json = Record<string, unknown>;
const stages = [['PLANNER', 'Requirement Planner'], ['MATCHING', 'Material Matching'], ['LOGISTICS', 'Logistics'], ['VALIDATION', 'Validation and Recommendation']] as const;

export function AgentTrace({ workflow }: { workflow: Workflow }) {
  return <section className="manager-panel agent-trace" aria-labelledby="agent-trace-heading">
    <div className="section-heading"><div><p className="eyebrow">Read-only audit</p><h2 id="agent-trace-heading">Agent Trace</h2></div><RequirementBadge status={workflow.status} /></div>
    <p className="muted">Saved stage results only. Buyer notes, addresses, coordinates, seller contact details, and provider payloads are not shown.</p>
    <dl className="detail-grid trace-overview"><div><dt>Workflow status</dt><dd><RequirementBadge status={workflow.status} /></dd></div><div><dt>Stage order</dt><dd>Planner → Matching → Logistics → Validation</dd></div><div><dt>Started</dt><dd>{requirementDate(workflow.startedAtUtc)}</dd></div><div><dt>Finished</dt><dd>{finishedAt(workflow.completedAtUtc, workflow.status)}</dd></div></dl>
    {workflow.steps.length === 0 ? <p className="empty-state">This workflow was completed before agent-trace persistence was enabled. Run a new matching workflow to view its agent trace.</p> : <><>{workflow.traceSourceWorkflowId && <p className="muted">Showing the persisted matching trace from workflow {shortRef(workflow.traceSourceWorkflowId)}.</p>}</><div className="agent-trace-stages">{stages.map(([stage, title], index) => <TraceCard key={stage} order={index + 1} stage={stage} title={title} step={workflow.steps.find(item => item.stage === stage)} workflowStatus={workflow.status} />)}</div></>}
  </section>;
}

function TraceCard({ order, stage, title, step, workflowStatus }: { order: number; stage: string; title: string; step?: AgentStep; workflowStatus: string }) {
  const output = object(parse(step?.outputJson));
  return <details className="agent-trace-card" open><summary><span className="trace-order">{order}</span><span><strong>{title}</strong><small>{stage}</small></span>{step ? <RequirementBadge status={step.status} /> : <span className="status-badge">NOT RECORDED</span>}</summary>
    {!step ? <p className="empty-state">This stage was not persisted for this workflow.</p> : <div className="trace-card-body"><dl className="detail-grid trace-metrics"><div><dt>Status</dt><dd><RequirementBadge status={step.status} /></dd></div><div><dt>Duration</dt><dd>{duration(step.durationMilliseconds)}</dd></div><div><dt>Retries</dt><dd>{step.retryCount}</dd></div><div><dt>Finished</dt><dd>{finishedAt(step.completedAtUtc, workflowStatus)}</dd></div>{step.errorJson && <div><dt>Error code</dt><dd>{errorCode(step.errorJson)}</dd></div>}</dl>
      {stage === 'PLANNER' && <Planner output={output} status={step.status} />}{stage === 'MATCHING' && <Matching output={output} />}{stage === 'LOGISTICS' && <Logistics output={output} />}{stage === 'VALIDATION' && <Validation output={output} tools={step.toolCalls} />}</div>}
  </details>;
}

function Planner({ output, status }: { output: Json; status: string }) {
  const criteria = object(output.normalizedCriteria);
  const checks = plannerChecks(criteria);
  const outcome = plannerOutcome(status, output, criteria, checks);
  if (outcome.kind === 'unavailable') return <><PlanningResult outcome={outcome} /><p className="empty-state">No normalized requirement data was persisted for this Planner stage.</p><PlannerChecks checks={checks} /></>;
  const item = text(criteria.itemName) ?? 'Not specified';
  const template = text(criteria.constructionItemTemplateId);
  const normalized = normalizedQuantity(criteria);
  return <><PlanningResult outcome={outcome} />
    {outcome.kind === 'ready' && <div className="trace-summary"><p>{`Buyer requires ${quantity(criteria.requiredQuantity, criteria.unit)} ${item} by ${date(criteria.deadline)}, with a maximum budget of ${money(criteria.maximumBudget)}.`}</p><p>{`The requirement was normalized to ${normalized} and is ready for material matching.`}</p></div>}
    <TraceDetails title="Handoff to Material Matching"><div><dt>Selected item</dt><dd>{item}</dd></div><div><dt>Category</dt><dd>{text(criteria.category) ?? 'Not specified'}</dd></div><div><dt>Required quantity</dt><dd>{quantity(criteria.requiredQuantity, criteria.unit)}</dd></div>{hasNormalizedQuantity(criteria) && <div><dt>Normalized/base quantity</dt><dd>{normalized}</dd></div>}<div><dt>Maximum budget</dt><dd>{money(criteria.maximumBudget)}</dd></div><div><dt>Deadline</dt><dd>{date(criteria.deadline)}</dd></div>{hasPackageMetadata(criteria) && <div><dt>Package metadata</dt><dd>{packageMetadata(criteria)}</dd></div>}</TraceDetails>
    <PlannerChecks checks={checks} />
    {template && <details className="trace-technical"><summary>Technical details</summary><dl className="detail-grid trace-details"><div><dt>Template reference</dt><dd>{shortRef(template)}</dd></div></dl></details>}
  </>;
}

type PlannerCheck = { label: string; passed: boolean };
type PlannerOutcome = { kind: 'ready' | 'clarification' | 'unavailable'; title: string; message: string };
function plannerChecks(criteria: Json): PlannerCheck[] {
  const quantityReady = isNumber(criteria.requiredQuantity) && Boolean(text(criteria.unit));
  return [
    { label: 'Selected item or template available', passed: Boolean(text(criteria.itemName) || text(criteria.constructionItemTemplateId)) },
    { label: 'Quantity and unit normalized', passed: quantityReady },
    { label: 'Budget available', passed: isNumber(criteria.maximumBudget) },
    { label: 'Deadline available', passed: Boolean(text(criteria.deadline)) },
    { label: 'All required matching criteria ready', passed: Boolean(text(criteria.category) || text(criteria.categoryId)) && quantityReady && isNumber(criteria.maximumBudget) && Boolean(text(criteria.deadline)) },
  ];
}
function plannerOutcome(status: string, output: Json, criteria: Json, checks: PlannerCheck[]): PlannerOutcome {
  const persistedStatus = text(output.status)?.toLowerCase();
  const invalid = status === 'REJECTED' || persistedStatus === 'invalid_input' || persistedStatus === 'needs_clarification' || Array.isArray(output.issues) && output.issues.length > 0;
  if (invalid) return { kind: 'clarification', title: 'Needs clarification', message: 'The persisted Planner result indicates that requirement data needs clarification before matching.' };
  if (status === 'COMPLETED' && Object.keys(criteria).length > 0 && checks.every(check => check.passed))
    return { kind: 'ready', title: 'Ready for matching', message: 'Normalized criteria are available for Material Matching.' };
  return { kind: 'unavailable', title: 'Planning unavailable', message: 'A complete persisted Planner result is not available for this workflow.' };
}
function PlanningResult({ outcome }: { outcome: PlannerOutcome }) { return <section className="trace-planning-result" aria-label="Planning result"><h3>Planning result</h3><div><RequirementBadge status={outcome.kind === 'ready' ? 'COMPLETED' : outcome.kind === 'clarification' ? 'WARNING' : 'NOT_RECORDED'} /><strong>{outcome.title}</strong></div><p className="muted">{outcome.message}</p></section>; }
function PlannerChecks({ checks }: { checks: PlannerCheck[] }) { return <><h3>Planner checks</h3><ul className="trace-list planner-checks">{checks.map(check => <li key={check.label}><strong>{check.label}</strong><RequirementBadge status={check.passed ? 'COMPLETED' : 'WARNING'} /><span>{check.passed ? 'Available in the persisted normalized criteria.' : 'Not available in the persisted normalized criteria.'}</span></li>)}</ul></>; }

function Matching({ output }: { output: Json }) {
  const candidates = objects(output.candidates); const exclusions = objects(output.exclusions);
  return <><TraceDetails title="Candidate results"><div><dt>Candidates</dt><dd>{candidates.length}</dd></div><div><dt>Exclusions</dt><dd>{exclusions.length}</dd></div><div><dt>Result</dt><dd>{label(text(output.status) ?? 'not recorded')}</dd></div></TraceDetails>{candidates.length === 0 ? <p className="empty-state">No candidate summaries were recorded.</p> : <ul className="trace-list">{candidates.map((candidate, index) => <li key={text(candidate.listingId) ?? index}><strong>Listing {shortRef(candidate.listingId)}</strong><span>{text(candidate.condition) ?? 'Condition unavailable'} · {quantity(candidate.availableQuantity, candidate.baseUnit ?? candidate.unit)} · {money(candidate.unitPrice)} per unit</span><span>Fit score: {number(candidate.basicFitScore)} · {text(candidate.reason) ?? 'No matching reason recorded'}</span></li>)}</ul>}{exclusions.length > 0 && <p className="muted">Exclusions: {exclusions.map(item => `${shortRef(item.listingId)} (${label(text(item.code) ?? 'excluded')})`).join(', ')}</p>}</>;
}

function Logistics({ output }: { output: Json }) {
  const candidates = objects(output.candidates);
  return candidates.length === 0 ? <p className="empty-state">No route results were recorded.</p> : <ul className="trace-list">{candidates.map((candidate, index) => <li key={text(candidate.listingId) ?? index}><strong>Listing {shortRef(candidate.listingId)}</strong><span>Delivery: {candidate.deliveryFeasible === true ? 'Feasible' : candidate.deliveryFeasible === false ? 'Not feasible' : 'Unavailable'} · Distance: {number(candidate.distanceKm, ' km')} · Duration: {number(candidate.durationMinutes, ' min')} · Transport: {money(candidate.estimatedTransportCost)}</span><span>{label(text(candidate.reason) ?? 'no result')}{warningText(candidate)}</span></li>)}</ul>;
}

function Validation({ output, tools }: { output: Json; tools: ToolCall[] }) {
  const breakdown = object(output.scoreBreakdown); const violations = strings(output.violations); const warnings = strings(output.warnings);
  return <><TraceDetails title="Recommendation"><div><dt>Validation</dt><dd>{output.valid === true ? 'Passed' : output.valid === false ? 'Failed' : 'Not recorded'}</dd></div><div><dt>Recommended match</dt><dd>{shortRef(output.recommendedMatchId)}</dd></div><div><dt>Manager approval</dt><dd>{output.requiresApproval === true ? 'Required' : 'Not available'}</dd></div><div><dt>Final score</dt><dd>{number(breakdown.score)}</dd></div><div><dt>Total estimated cost</dt><dd>{money(breakdown.totalEstimatedCost)}</dd></div></TraceDetails><h3>Validation checks</h3><ul className="trace-list validation-checks">{tools.length === 0 ? <li>No validation tool results were recorded.</li> : tools.map(tool => <li key={tool.id}><strong>{label(tool.toolName)}</strong><RequirementBadge status={tool.errorJson ? 'FAILED' : passed(tool.outputJson) ? 'COMPLETED' : 'REJECTED'} /><span>{tool.errorJson ? errorCode(tool.errorJson) : checkCode(tool.outputJson)} · {duration(tool.durationMilliseconds)} · {tool.retryCount} retries</span></li>)}</ul>{(violations.length > 0 || warnings.length > 0) && <div className="trace-alerts">{violations.map(value => <p className="error-message" key={value}>Violation: {label(value)}</p>)}{warnings.map(value => <p className="decision-notice" key={value}>Warning: {label(value)}</p>)}</div>}</>;
}

function TraceDetails({ title, children }: { title: string; children: ReactNode }) { return <><h3>{title}</h3><dl className="detail-grid trace-details">{children}</dl></>; }
function parse(value: string | undefined): unknown { try { return value ? JSON.parse(value) : {}; } catch { return {}; } }
function object(value: unknown): Json { return value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Json : {}; }
function objects(value: unknown): Json[] { return Array.isArray(value) ? value.map(object).filter(item => Object.keys(item).length > 0) : []; }
function strings(value: unknown): string[] { return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : []; }
function text(value: unknown): string | null { return typeof value === 'string' && value.trim() ? value : null; }
function isNumber(value: unknown): boolean { return typeof value === 'number' || typeof value === 'string' && value !== '' && Number.isFinite(Number(value)); }
function number(value: unknown, suffix = ''): string { return typeof value === 'number' || typeof value === 'string' && value !== '' && Number.isFinite(Number(value)) ? `${requirementNumber(Number(value), 2)}${suffix}` : 'Not available'; }
function quantity(value: unknown, unit: unknown): string { const amount = number(value); const suffix = text(unit); return amount === 'Not available' ? amount : `${amount}${suffix ? ` ${suffix}` : ''}`; }
function money(value: unknown): string { return typeof value === 'number' || typeof value === 'string' && value !== '' && Number.isFinite(Number(value)) ? formatLkr(Number(value)) : 'Not available'; }
function date(value: unknown): string { const input = text(value); return input ? requirementDate(input) : 'Not available'; }
function finishedAt(value: string | null, status: string): string { if (value) return requirementDate(value); return ['APPROVED', 'COMPLETED', 'FAILED', 'REJECTED', 'REVISION_REQUESTED'].includes(status) ? 'Not recorded' : 'In progress'; }
function duration(value: number | null): string { return value === null ? 'Unavailable' : value < 1000 ? `${value} ms` : `${(value / 1000).toFixed(1)} s`; }
function shortRef(value: unknown): string { const result = text(value); return result ? result.slice(0, 8) : 'Not available'; }
function label(value: string): string { return statusLabel(value); }
function errorCode(value: string): string { const parsed = object(parse(value)); return text(parsed.errorCode) ?? text(parsed.code) ?? 'Recorded failure'; }
function passed(value: string): boolean { return object(parse(value)).passed === true; }
function checkCode(value: string): string { const result = object(parse(value)); return label(text(result.code) ?? (result.passed === true ? 'CHECK_PASSED' : 'CHECK_FAILED')); }
function warningText(candidate: Json): string { const warnings = objects(candidate.warnings).map(item => label(text(item.code) ?? 'warning')); return warnings.length ? ` · Warnings: ${warnings.join(', ')}` : ''; }
function hasNormalizedQuantity(criteria: Json): boolean { return isNumber(criteria.normalizedRequiredQuantity) && Boolean(text(criteria.normalizedBaseUnit) ?? text(criteria.baseUnit)); }
function normalizedQuantity(criteria: Json): string { return hasNormalizedQuantity(criteria) ? quantity(criteria.normalizedRequiredQuantity, criteria.normalizedBaseUnit ?? criteria.baseUnit) : quantity(criteria.requiredQuantity, criteria.baseUnit ?? criteria.unit); }
function hasPackageMetadata(criteria: Json): boolean { const inputMode = text(criteria.inputMode)?.toUpperCase(); return inputMode === 'PACKAGE_COUNT' || inputMode === 'PACKAGE' || inputMode === 'PIECE' || isNumber(criteria.preferredPackageSize) || Boolean(text(criteria.packageBaseUnit)); }
function packageMetadata(criteria: Json): string { const values = [text(criteria.inputMode), quantity(criteria.preferredPackageSize, criteria.packageBaseUnit), text(criteria.enteredUnit)]; const result = values.filter(value => value && value !== 'Not available'); return result.length ? result.join(' · ') : 'Not recorded'; }

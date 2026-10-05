import { WorkflowSummary } from './WorkflowSummary';
import { WorkflowTransactions } from './WorkflowTransactions';
import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { RequirementBadge, RequirementError, requirementDate, useRequirementResource } from '../../features/requirements/requirementUi';
import { managerWorkflowsApi, type ManagerWorkflowsApi, type Workflow } from '../../features/workflows/managerWorkflowsApi';
import { invalidateLiveData } from '../../hooks/useLiveResource';
import { SuccessOverlay } from '../../components/StatusAnimation';
import { AgentTrace } from './AgentTrace';

export function ManagerWorkflowDetailsPage({ api = managerWorkflowsApi }: { api?: ManagerWorkflowsApi }) {
  const { workflowId = '' } = useParams(); const load = useCallback(() => api.get(workflowId), [api, workflowId]);
  const resource = useRequirementResource(load);
  const [note, setNote] = useState(''); const [busy, setBusy] = useState(false); const [decisionError, setDecisionError] = useState(''); const [notice, setNotice] = useState('');
  const [showApprovalSuccess, setShowApprovalSuccess] = useState(false);
  const workflow = resource.data;
  async function decide(action: 'approve' | 'reject' | 'revise') {
    if ((action === 'reject' || action === 'revise') && !note.trim()) { setDecisionError('A note is required for rejection or a revision request.'); return; }
    setBusy(true); setDecisionError(''); setNotice('');
    try { const result = await api[action](workflowId, note); invalidateLiveData(); setNote(''); setNotice(action === 'approve' ? 'Approved. A reservation outcome is shown below.' : action === 'reject' ? 'Rejected. No reservation was created.' : 'Revision requested. The buyer can restart matching after reviewing the note.'); await resource.reload(); if (result.status === 'APPROVED') { setNotice('Approved. Reservation created for the matched inventory.'); setShowApprovalSuccess(true); } }
    catch (error) { setDecisionError(error instanceof Error ? error.message : 'Unable to save this decision.'); }
    finally { setBusy(false); }
  }
  return <div className="manager-page workflow-page"><Link className="back-link" to="/app/manager/requirement-approvals">Back to Buyer Requirement Approvals</Link><header className="page-heading"><div><p className="eyebrow">Manager review</p><h1>Buyer Requirement Approval Details</h1></div><button className="button button-secondary" type="button" onClick={resource.reload} disabled={resource.loading || busy}>Refresh</button></header>
    {showApprovalSuccess && <SuccessOverlay kind="approval" title="Approved successfully" message="Buyer requirement approved." onComplete={() => setShowApprovalSuccess(false)} />}
    {resource.loading && <p role="status">Loading workflow...</p>}{resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
    {workflow && <><section className="manager-panel"><div className="section-heading"><h2>Review summary</h2><RequirementBadge status={workflow.status} /></div><WorkflowSummary key={workflow.id + workflow.status + workflow.outputJson} workflow={workflow} /><small className="requirement-id" title={workflow.id}>Workflow reference: {workflow.id.slice(0, 8)}</small></section>
      <AgentTrace workflow={workflow} />
      {notice && <p className="decision-notice" role="status">{notice}</p>}
      {!canDecide(workflow) && <Outcome workflow={workflow} />}
      {workflow.materialMatchId && <WorkflowTransactions key={workflow.id + workflow.status} matchId={workflow.materialMatchId} api={api} onComplete={async () => { await resource.reload(); }} />}
      <details className="manager-panel"><summary>Technical details</summary><dl className="detail-grid"><div><dt>Current stage</dt><dd>{workflow.currentStage}</dd></div><div><dt>Started</dt><dd>{requirementDate(workflow.startedAtUtc)}</dd></div><div><dt>Execution finished</dt><dd>{workflow.completedAtUtc ? requirementDate(workflow.completedAtUtc) : finishedAt(workflow.status)}</dd></div></dl><p className="muted">Sensitive raw inputs and provider payloads are intentionally not displayed.</p></details>
      {canDecide(workflow) && <section className="manager-panel" aria-labelledby="decision-heading"><h2 id="decision-heading">Manager decision</h2><label className="decision-note">Decision note<textarea value={note} maxLength={2000} onChange={(event) => setNote(event.target.value)} placeholder="Required for reject or revision request" /></label>{decisionError && <p className="field-error" role="alert">{decisionError}</p>}<div className="action-row"><button type="button" className="button button-primary" disabled={busy} onClick={() => void decide('approve')}>Approve</button><button type="button" className="button button-danger" disabled={busy} onClick={() => void decide('reject')}>Reject</button><button type="button" className="button button-secondary" disabled={busy} onClick={() => void decide('revise')}>Request revision</button></div></section>}
      <section className="manager-panel"><h2>Decision audit & history</h2>{workflow.approvals.length === 0 ? <p className="empty-state">No manager decisions have been recorded.</p> : <ol className="history-list">{workflow.approvals.map((approval) => <li key={approval.id}><strong>{approval.decision.replaceAll('_', ' ')}</strong><span>{requirementDate(approval.decidedAtUtc)} / Manager decision</span>{approval.note && <span>{approval.note}</span>}</li>)}</ol>}</section>
    </>}</div>;
}
function Outcome({ workflow }: { workflow: Workflow }) { const text = workflow.status === 'APPROVED' ? 'Approved: reservation created for the matched inventory.' : workflow.status === 'REJECTED' ? 'Rejected: no reservation was created.' : workflow.status === 'REVISION_REQUESTED' ? 'Revision requested: the buyer must restart matching after reviewing the note.' : 'No manager action is available at this stage.'; return <p className="decision-notice">{text}{workflow.decision ? ' Note: ' + workflow.decision : ''}</p>; }
function canDecide(workflow: Workflow) { return workflow.status === 'PENDING_APPROVAL'; }
function finishedAt(status: Workflow['status']) { return ['APPROVED', 'COMPLETED', 'FAILED', 'REJECTED', 'REVISION_REQUESTED'].includes(status) ? 'Not recorded' : 'In progress'; }

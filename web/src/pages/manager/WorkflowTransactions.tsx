import { useCallback, useState } from 'react';
import { RequirementBadge, RequirementError, RequirementPagination, requirementDate, requirementNumber, useRequirementResource } from '../../features/requirements/requirementUi';
import type { ManagerWorkflowsApi } from '../../features/workflows/managerWorkflowsApi';

export function WorkflowTransactions({ matchId, api, onComplete }: { matchId: string; api: ManagerWorkflowsApi; onComplete: () => Promise<void> }) {
  const [page, setPage] = useState(1);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [historyId, setHistoryId] = useState<string | null>(null);
  const resource = useRequirementResource(useCallback(() => api.transactions(matchId, page), [api, matchId, page]));
  async function complete(id: string) {
    setBusy(true); setError('');
    try { await api.completeTransaction(id); await resource.reload(); await onComplete(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to complete transaction.'); }
    finally { setBusy(false); }
  }
  return <section className="manager-panel"><div className="section-heading"><h2>Transactions and reservations for this match</h2><button className="button button-secondary" onClick={resource.reload} disabled={busy || resource.loading}>Refresh transactions</button></div>
    {resource.loading && <p role="status">Loading transactions...</p>}
    {resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
    {error && <p role="alert" className="field-error">{error}</p>}
    {resource.data && <>{resource.data.items.length === 0 ? <p className="empty-state">No transaction records for this match.</p> : <div className="table-scroll"><table><thead><tr><th>Transaction</th><th>Status</th><th>Quantity</th><th>Reserved</th><th>Material value (LKR)</th><th>Actions</th></tr></thead><tbody>{resource.data.items.map(row => <tr key={row.id}><td>{row.id.slice(0, 8)}</td><td><RequirementBadge status={row.status} /></td><td>{requirementNumber(row.quantity)}</td><td>{requirementNumber(row.reservedQuantity)}</td><td>{requirementNumber(row.totalValue, 2)}</td><td><button className="text-button" onClick={() => setHistoryId(row.id)}>History</button>{row.status === 'APPROVED' && <button className="button button-primary" disabled={busy} onClick={() => void complete(row.id)}>Complete transfer</button>}</td></tr>)}</tbody></table></div>}
      <RequirementPagination page={resource.data.page} pageSize={resource.data.pageSize} total={resource.data.total} onPage={setPage} /></>}
    {historyId && <TransactionHistory key={historyId} id={historyId} api={api} />}
  </section>;
}
function TransactionHistory({ id, api }: { id: string; api: ManagerWorkflowsApi }) {
  const [page, setPage] = useState(1);
  const resource = useRequirementResource(useCallback(() => api.transactionHistory(id, page), [api, id, page]));
  return <div><h3>Transaction history</h3>{resource.loading && <p role="status">Loading history...</p>}{resource.error && <RequirementError message={resource.error} retry={resource.reload} />}
    {resource.data && <><ol className="history-list">{resource.data.items.map(row => <li key={row.id}><strong>{row.action.replaceAll('_', ' ')}</strong><span>{requirementDate(row.createdAt)}</span></li>)}</ol>{resource.data.items.length === 0 && <p>No history events yet.</p>}
      <div className="pagination"><button disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous history</button><span>Page {page} of {Math.max(1, resource.data.totalPages)}</span><button disabled={page >= resource.data.totalPages} onClick={() => setPage(page + 1)}>Next history</button></div></>}
  </div>;
}

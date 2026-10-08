import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { formatLkr } from '../../utils/currency';
import { transactionConfirmationsApi, transactionsApi, type Offer, type Transaction, type TransactionHistoryEntry } from '../../features/transactions/transactionsApi';

export function ManagerTransactionDetailsPage() {
  const { transactionId = '' } = useParams();
  const [transaction, setTransaction] = useState<Transaction | null>(null);
  const [offer, setOffer] = useState<Offer | null>(null);
  const [history, setHistory] = useState<TransactionHistoryEntry[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState('');

  async function load() {
    if (!transactionId) return;
    setError('');
    try {
      const current = await transactionsApi.transaction!(transactionId);
      const [currentOffer, audit] = await Promise.all([transactionsApi.offer(current.offerId), transactionsApi.history(transactionId, 1)]);
      setTransaction(current); setOffer(currentOffer); setHistory(audit.items);
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to load this transaction.'); }
  }
  useEffect(() => { void load(); }, [transactionId]);

  async function resolve(kind: 'completed' | 'not-completed') {
    if (!transaction || busy) return;
    if (kind === 'not-completed' && !note.trim()) { setError('A manager note is required when marking a transaction not completed.'); return; }
    const message = kind === 'completed' ? 'Mark this transaction completed after follow-up? Buyer receipt will not be recorded.' : 'Mark this transaction not completed and release its reservation?';
    if (!window.confirm(message)) return;
    setBusy(true); setError('');
    try {
      if (kind === 'completed') await transactionConfirmationsApi.resolveCompleted(transaction.id, note.trim() || undefined);
      else await transactionConfirmationsApi.resolveNotCompleted(transaction.id, note.trim());
      setNote(''); await load();
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to resolve this transaction.'); }
    finally { setBusy(false); }
  }

  if (!transaction && !error) return <section className="manager-panel" aria-live="polite">Loading transaction...</section>;
  if (!transaction) return <section className="manager-panel"><p className="error-message" role="alert">{error}</p><button className="button button-secondary" onClick={() => void load()}>Retry</button></section>;
  const eligible = ['APPROVED', 'HANDED_OVER', 'MANAGER_REVIEW_REQUIRED'].includes(transaction.status);
  return <div className="manager-page">
    <Link className="back-link" to="/app/manager">Back to Manager Dashboard</Link>
    <header className="page-heading"><p className="eyebrow">Transaction follow-up</p><h1>{reference(transaction.id)}</h1><p className="muted">{offer?.materialName ?? 'Material details loading'}</p></header>
    {error && <p className="error-message" role="alert">{error}</p>}
    <section className="manager-panel"><div className="section-heading"><h2>Transaction details</h2><span className="status-badge">{transaction.status.replaceAll('_', ' ')}</span></div>
      <dl className="detail-grid"><Detail label="Material" value={offer?.materialName} /><Detail label="Offer" value={offer?.id} /><Detail label="Quantity" value={`${transaction.quantity} ${offer?.unit ?? ''}`} /><Detail label="Total value" value={formatLkr(transaction.totalValue)} /><Detail label="Manager approved" value={date(transaction.managerApprovedAt)} /><Detail label="Confirmation deadline" value={date(transaction.confirmationDeadline)} /><Detail label="Seller handed over" value={date(transaction.sellerHandoverConfirmedAt)} /><Detail label="Buyer received" value={date(transaction.buyerReceivedConfirmedAt)} /><Detail label="Resolved" value={date(transaction.resolvedAt)} /></dl>
      {transaction.resolutionNote && <p><strong>Resolution note:</strong> {transaction.resolutionNote}</p>}
    </section>
    <section className="manager-panel"><h2>Contacts</h2><div className="contact-grid"><Contact title="Buyer" contact={transaction.buyerContact} /><Contact title="Seller" contact={transaction.sellerContact} /></div></section>
    {eligible && <section className="manager-panel"><h2>Manager resolution</h2><label className="decision-note">Follow-up note (required for not completed)<textarea value={note} maxLength={500} disabled={busy} onChange={event => setNote(event.target.value)} /></label><div className="action-row"><button className="button button-primary" disabled={busy} onClick={() => void resolve('completed')}>Mark completed after follow-up</button><button className="button button-danger" disabled={busy} onClick={() => void resolve('not-completed')}>Mark not completed</button></div></section>}
    <section className="manager-panel"><h2>Transaction history</h2>{history.length === 0 ? <p className="empty-state">No history recorded.</p> : <ol className="history-list">{history.map(item => <li key={item.id}><strong>{item.action.replaceAll('_', ' ')}</strong><span>{date(item.createdAt)}{item.note ? ` — ${item.note}` : ''}</span></li>)}</ol>}</section>
  </div>;
}
function Detail({ label, value }: { label: string; value?: string | number | null }) { return <div><dt>{label}</dt><dd>{value || 'Not recorded'}</dd></div>; }
function Contact({ title, contact }: { title: string; contact?: { fullName: string | null; email: string; phoneNumber: string | null } | null }) { return <article className="contact-card"><h3>{title}</h3><p>{contact?.fullName ?? 'Not recorded'}</p><p>{contact?.email ?? 'Not recorded'}</p><p>{contact?.phoneNumber ?? 'Not recorded'}</p></article>; }
function date(value?: string | null) { return value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : 'Not recorded'; }
function reference(id: string) { return `TX-${id.replaceAll('-', '').slice(0, 8).toUpperCase()}`; }

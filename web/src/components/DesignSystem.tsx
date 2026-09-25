import type { ReactNode } from 'react';

export function PageHeader({ eyebrow, title, children }: { eyebrow?: string; title: string; children?: ReactNode }) {
  return <header className="page-heading"><div>{eyebrow && <p className="eyebrow">{eyebrow}</p>}<h1>{title}</h1>{children}</div></header>;
}
export function SectionCard({ title, action, children }: { title: string; action?: ReactNode; children: ReactNode }) {
  return <section className="manager-panel section-card" aria-label={title}><div className="section-heading"><h2>{title}</h2>{action}</div>{children}</section>;
}
export function StatusBadge({ status }: { status: string }) { return <span className="status-badge" data-status={status}>{status.replaceAll('_', ' ')}</span>; }
export function EmptyState({ children }: { children: ReactNode }) { return <p className="empty-state">{children}</p>; }
export function LoadingSkeleton({ rows = 3 }: { rows?: number }) { return <div className="loading-skeleton" role="status" aria-label="Loading content">{Array.from({ length: rows }, (_, i) => <i key={i} />)}</div>; }
export function MetricTrend({ label }: { label: string }) { return <small className="metric-trend"><span aria-hidden="true">↗</span>{label}</small>; }
export function LiveIndicator({ updatedAt }: { updatedAt: Date | null }) { return <span className="live-indicator" aria-live="polite"><i aria-hidden="true" />Live <b>•</b> {updatedAt ? 'Updated just now' : 'Connecting'}</span>; }
export function ApprovalCard({ children }: { children: ReactNode }) { return <div className="approval-card">{children}</div>; }

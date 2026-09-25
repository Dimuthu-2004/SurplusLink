import { useEffect, useState } from 'react';
import { ApiError } from '../../api/apiClient';

export function useRequirementResource<T>(load: () => Promise<T>) {
  const [state, setState] = useState<{ data: T | null; error: string | null; loading: boolean }>({
    data: null, error: null, loading: true,
  });
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    let active = true;
    setState({ data: null, error: null, loading: true });
    void load().then(
      (data) => { if (active) setState({ data, error: null, loading: false }); },
      (error: unknown) => { if (active) setState({ data: null, error: requirementError(error), loading: false }); },
    );
    return () => { active = false; };
  }, [load, revision]);
  return { ...state, reload: () => setRevision((value) => value + 1) };
}
function requirementError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 403) return 'You do not have permission to view these requirements.';
    if (error.status === 404) return 'This requirement could not be found.';
    if (error.validationErrors) return Object.values(error.validationErrors).flat().join(' ');
  }
  return error instanceof Error ? error.message : 'Unable to load requirements. Please try again.';
}
export function RequirementError({ message, retry, label = 'Retry' }: { message: string; retry: () => void; label?: string }) {
  return <div className="error-message" role="alert"><p>{message}</p>
    <button className="button button-secondary" onClick={retry} type="button">{label}</button></div>;
}
export function statusLabel(value: string) {
  return value.toLowerCase().split('_').map((word) => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
}
export function RequirementBadge({ status }: { status: string }) {
  return <span className="status-badge" data-status={status}>{statusLabel(status)}</span>;
}
export function requirementDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Unavailable' :
    new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(date);
}
export function requirementNumber(value: number, digits = 3) {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: digits }).format(value);
}
export function RequirementPagination({ page, total, pageSize, onPage }: {
  page: number; total: number; pageSize: number; onPage: (page: number) => void;
}) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return <nav className="pagination" aria-label="Results pagination">
    <button type="button" className="button button-secondary" disabled={page <= 1} onClick={() => onPage(page - 1)}>Previous</button>
    <span>Page {page} of {pages}</span>
    <button type="button" className="button button-secondary" disabled={page >= pages} onClick={() => onPage(page + 1)}>Next</button>
  </nav>;
}

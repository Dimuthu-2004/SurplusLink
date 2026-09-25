import { useCallback, useEffect, useRef, useState } from 'react';

export const liveDataInvalidatedEvent = 'surpluslink:live-data-invalidated';

/** Notify mounted read views after a successful mutation. */
export function invalidateLiveData() {
  window.dispatchEvent(new Event(liveDataInvalidatedEvent));
}

export interface LiveResource<T> {
  data: T | null;
  error: string | null;
  loading: boolean;
  updatedAt: Date | null;
  reload: () => Promise<void>;
}

/** One in-flight request, 10-second polling, focus refresh, and mutation refresh. */
export function useLiveResource<T>(load: () => Promise<T>, intervalMs = 10_000): LiveResource<T> {
  const [state, setState] = useState<Omit<LiveResource<T>, 'reload'>>({ data: null, error: null, loading: true, updatedAt: null });
  const inFlight = useRef(false);
  const reload = useCallback(async () => {
    if (inFlight.current) return;
    inFlight.current = true;
    setState(previous => ({ ...previous, loading: previous.data === null, error: null }));
    try {
      const data = await load();
      setState({ data, error: null, loading: false, updatedAt: new Date() });
    } catch (error) {
      setState(previous => ({ ...previous, error: error instanceof Error ? error.message : 'Unable to load this information.', loading: false }));
    } finally { inFlight.current = false; }
  }, [load]);
  useEffect(() => {
    void reload();
    const refreshWhenVisible = () => { if (document.visibilityState === 'visible') void reload(); };
    const timer = window.setInterval(refreshWhenVisible, intervalMs);
    window.addEventListener('focus', refreshWhenVisible);
    window.addEventListener(liveDataInvalidatedEvent, refreshWhenVisible);
    return () => { window.clearInterval(timer); window.removeEventListener('focus', refreshWhenVisible); window.removeEventListener(liveDataInvalidatedEvent, refreshWhenVisible); };
  }, [intervalMs, reload]);
  return { ...state, reload };
}

/** Adds bounded refresh behaviour to an existing local resource without changing its API. */
export function useLiveReload(reload: () => void, loading: boolean, enabled = true, intervalMs = 10_000) {
  const latestReload = useRef(reload); const latestLoading = useRef(loading); const lastRefresh = useRef(0);
  useEffect(() => { latestReload.current = reload; });
  useEffect(() => { latestLoading.current = loading; }, [loading]);
  useEffect(() => {
    if (!enabled) return;
    const refreshWhenVisible = () => {
      if (document.visibilityState !== 'visible' || latestLoading.current) return;
      const now = Date.now();
      if (now - lastRefresh.current < 750) return;
      lastRefresh.current = now; latestReload.current();
    };
    const timer = window.setInterval(refreshWhenVisible, intervalMs);
    window.addEventListener('focus', refreshWhenVisible);
    window.addEventListener(liveDataInvalidatedEvent, refreshWhenVisible);
    return () => { window.clearInterval(timer); window.removeEventListener('focus', refreshWhenVisible); window.removeEventListener(liveDataInvalidatedEvent, refreshWhenVisible); };
  }, [enabled, intervalMs]);
}

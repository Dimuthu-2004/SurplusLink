import { act, render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { invalidateLiveData, useLiveResource } from '../hooks/useLiveResource';

afterEach(() => vi.useRealTimers());

it('polls while mounted, refreshes on focus, and refetches immediately after a mutation invalidation', async () => {
  vi.useFakeTimers();
  const load = vi.fn(async () => load.mock.calls.length);
  function Probe() {
    const resource = useLiveResource(load, 10_000);
    return <output>{resource.data ?? 'loading'}</output>;
  }
  const view = render(<Probe />);
  await act(async () => { await Promise.resolve(); });
  expect(screen.getByText('1')).toBeInTheDocument();

  await act(async () => { await vi.advanceTimersByTimeAsync(10_000); });
  expect(screen.getByText('2')).toBeInTheDocument();
  await act(async () => { window.dispatchEvent(new Event('focus')); await Promise.resolve(); });
  expect(screen.getByText('3')).toBeInTheDocument();
  await act(async () => { invalidateLiveData(); await Promise.resolve(); });
  expect(screen.getByText('4')).toBeInTheDocument();

  view.unmount();
  await act(async () => { await vi.advanceTimersByTimeAsync(20_000); });
  expect(load).toHaveBeenCalledTimes(4);
});

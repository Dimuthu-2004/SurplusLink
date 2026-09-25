import { act, render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { AnalyticsMetric } from '../components/AnalyticsChart';
afterEach(() => vi.restoreAllMocks());
it('animates numeric metrics from zero and exposes the final value accessibly', () => {
  let frame!: FrameRequestCallback;
  vi.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => { frame = callback; return 1; });
  vi.spyOn(performance, 'now').mockReturnValue(0);
  const cancel = vi.spyOn(window, 'cancelAnimationFrame');
  const { unmount } = render(<AnalyticsMetric label="Total" value={1234} />);
  expect(screen.getByLabelText('1234')).toBeInTheDocument();
  expect(screen.getByText('0')).toHaveAttribute('aria-hidden', 'true');
  act(() => frame(650));
  expect(screen.getByText('1,234')).toBeInTheDocument();
  unmount();
  expect(cancel).toHaveBeenCalled();
});
it('respects reduced motion and retains nonnumeric unavailable states', () => {
  vi.stubGlobal('matchMedia', () => ({ matches: true }));
  const { rerender } = render(<AnalyticsMetric label="Total" value={1234} />);
  expect(screen.getByText('1,234')).toBeInTheDocument();
  rerender(<AnalyticsMetric label="Total" value="Unavailable" />);
  expect(screen.getByLabelText('Unavailable')).toBeInTheDocument();
  vi.unstubAllGlobals();
});

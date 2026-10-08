import { render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { SuccessOverlay } from '../components/StatusAnimation';

vi.mock('lottie-react', () => ({
  default: ({ autoplay, loop }: { autoplay: boolean; loop: boolean }) => (
    <div data-testid="approval-lottie" data-autoplay={String(autoplay)} data-loop={String(loop)} />
  ),
}));

it('renders the approval animation as a decorative looping acknowledgement', async () => {
  render(<SuccessOverlay kind="approval" title="Approved successfully" message="Seller listing approved." onComplete={vi.fn()} />);

  const animation = await screen.findByTestId('approval-lottie');
  expect(animation).toHaveAttribute('data-autoplay', 'true');
  expect(animation).toHaveAttribute('data-loop', 'true');
  expect(animation.parentElement).toHaveAttribute('aria-hidden', 'true');
  expect(animation.parentElement).toHaveStyle({ width: '170px' });
  expect(screen.getByRole('dialog')).toHaveTextContent('Approved successfully');
  expect(screen.getByRole('dialog')).toHaveTextContent('Seller listing approved.');
});

it('keeps a visible approval acknowledgement when reduced motion disables Lottie autoplay', async () => {
  const original = window.matchMedia;
  Object.defineProperty(window, 'matchMedia', { configurable: true, value: () => ({ matches: true, addEventListener: vi.fn(), removeEventListener: vi.fn() }) });
  render(<SuccessOverlay kind="approval" title="Approved successfully" message="Seller listing approved." onComplete={vi.fn()} />);
  expect(await screen.findByTestId('status-animation-fallback')).toHaveTextContent('🐦');
  Object.defineProperty(window, 'matchMedia', { configurable: true, value: original });
});

import { render, screen } from '@testing-library/react';
import { vi, expect, it } from 'vitest';
import { SubmittedAnimationOverlay, WorkflowRunningOverlay } from '../components/SubmissionAnimationOverlays';

vi.mock('lottie-react', () => ({
  default: ({ loop }: { loop: boolean }) => <div data-testid="lottie" data-loop={String(loop)} />,
}));

it('renders submitted state as a one-shot acknowledgement', () => {
  render(
    <SubmittedAnimationOverlay
      open
      title="Listing submitted"
      message="Your material was sent for manager review."
      onCompleted={vi.fn()}
    />,
  );

  expect(screen.getByRole('dialog')).toHaveTextContent('Listing submitted');
  expect(screen.getByTestId('lottie')).toHaveAttribute('data-loop', 'false');
});

it('renders workflow state as a persistent looping overlay', () => {
  render(
    <WorkflowRunningOverlay
      open
      title="Finding suitable matches..."
      message="Our matching workflow is checking available sellers."
    />,
  );

  expect(screen.getByRole('dialog')).toHaveTextContent('Finding suitable matches...');
  expect(screen.getByTestId('lottie')).toHaveAttribute('data-loop', 'true');
});

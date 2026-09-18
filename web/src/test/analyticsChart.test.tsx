import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { AnalyticsChart } from '../components/AnalyticsChart';

describe('analytics charts', () => {
  it('shows real counts and shares, and switches chart type accessibly', async () => {
    const user = userEvent.setup();
    render(<AnalyticsChart title="Inventory" values={[{ key: 'Cement', count: 3 }, { key: 'Steel', count: 1 }]} />);
    expect(screen.getByText('(75%)')).toBeInTheDocument();
    expect(screen.getByText('(25%)')).toBeInTheDocument();
    expect(within(screen.getByRole('list')).getAllByRole('listitem')).toHaveLength(2);
    await user.click(screen.getByRole('button', { name: 'Ring' }));
    expect(screen.getByRole('img', { name: 'Inventory: 4 total' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Ring' })).toHaveAttribute('aria-pressed', 'true');
    await user.click(screen.getByRole('button', { name: 'Bars' }));
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
  it('handles zero data without invalid chart proportions', () => {
    render(<AnalyticsChart title="Empty" values={[{ key: 'Open', count: 0 }]} />);
    expect(screen.getByText('No data yet.')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
});

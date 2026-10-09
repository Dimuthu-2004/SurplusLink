import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('lottie-react', () => ({
  default: () => {
    throw new Error('simulated animation renderer failure');
  },
}));

const clearError = vi.fn();
vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({
    login: vi.fn(), register: vi.fn(), verifyEmail: vi.fn(), resendVerification: vi.fn(),
    forgotPassword: vi.fn(), resetPassword: vi.fn(), pending: false, error: null,
    errorCode: null, clearError,
  }),
}));

import { LoginPage } from '../pages/LoginPage';

function renderRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<LoginPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('auth route regressions', () => {
  beforeEach(() => clearError.mockClear());

  it('renders login without any status or Lottie animation', async () => {
    renderRoute('/login');
    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeVisible();
    expect(screen.getByLabelText('Email')).toBeVisible();
    expect(screen.getByLabelText('Password')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeVisible();
    expect(document.querySelector('[data-approval-animation="true"]')).not.toBeInTheDocument();
    expect(document.querySelector('.status-animation')).not.toBeInTheDocument();
    expect(document.querySelector('[data-testid="approval-lottie"]')).not.toBeInTheDocument();
  });

  it('renders registration directly and completes the sign-in route transition visibly', async () => {
    const view = renderRoute('/register');
    const user = userEvent.setup();
    expect(await screen.findByRole('heading', { name: 'Build with less waste.' })).toBeVisible();
    expect(screen.getByLabelText('Full name')).toBeVisible();
    expect(document.querySelector('.status-animation')).not.toBeInTheDocument();
    expect(document.querySelector('[data-approval-animation="true"]')).not.toBeInTheDocument();

    await user.click(document.querySelector<HTMLButtonElement>('.auth-panel-action')!);
    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeVisible();
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Welcome back' }).closest('form')).not.toHaveStyle({ opacity: '0' }));
    await waitFor(() => expect(document.querySelector<HTMLButtonElement>('.auth-panel-action')).toBeEnabled());

    view.unmount();
    renderRoute('/register');
    expect(await screen.findByRole('heading', { name: 'Build with less waste.' })).toBeVisible();
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());
  });
});

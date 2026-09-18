import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/apiClient';
import { App } from '../app/App';
import { AuthProvider } from '../auth/AuthContext';
import type { AuthUser, UserRole } from '../auth/authTypes';
import type { TokenStorage } from '../auth/tokenStorage';

const seller = user('SELLER');
const buyer = user('BUYER');

describe('authentication navigation', () => {
  it('redirects an anonymous visitor away from protected routes', async () => {
    renderApp('/app/seller');

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(screen.queryByText('Seller home')).not.toBeInTheDocument();
  });

  it('restores a session and redirects a user away from another role', async () => {
    const storage = memoryStorage('stored-jwt');
    const client = fakeClient();
    client.get.mockResolvedValue({ data: seller });

    renderApp('/app/buyer', client, storage);

    expect(await screen.findByRole('heading', { name: 'Seller home' })).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Seller navigation' })).toBeInTheDocument();
    expect(screen.queryByText('Buyer home')).not.toBeInTheDocument();
  });

  it.each(['/app/manager', '/app/manager/categories', '/app/manager/requirements'])('blocks dual-role manager navigation at %s', async path => {
    const client = fakeClient();
    client.get.mockResolvedValue({ data: { ...seller, roles: ['SELLER', 'BUYER'] } });
    renderApp(path, client, memoryStorage('dual-token'));
    expect(await screen.findByRole('heading', { name: 'Seller home' })).toBeInTheDocument();
    expect(screen.queryByText('Material categories')).not.toBeInTheDocument();
    expect(screen.queryByText('Buyer Requirements')).not.toBeInTheDocument();
  });

  it('allows both marketplace routes for one dual-role session', async () => {
    const client = fakeClient();
    client.get.mockResolvedValue({ data: { ...seller, roles: ['SELLER', 'BUYER'] } });
    renderApp('/app/buyer', client, memoryStorage('dual-token'));
    expect(await screen.findByRole('heading', { name: 'Buyer home' })).toBeInTheDocument();
  });

  it('logs in through the API and opens the matching role home', async () => {
    const client = fakeClient();
    client.post.mockResolvedValue({ data: { token: 'signed-jwt', user: buyer } });
    const storage = memoryStorage();
    renderApp('/login', client, storage);
    const visitor = userEvent.setup();

    await visitor.type(await screen.findByLabelText('Email'), ' buyer@example.com ');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('heading', { name: 'Buyer home' })).toBeInTheDocument();
    expect(client.post).toHaveBeenCalledWith('/api/auth/login', {
      email: 'buyer@example.com',
      password: 'Password123!',
    });
    expect(storage.read()).toBe('signed-jwt');
  });

  it('shows loading and the normalized API error', async () => {
    const client = fakeClient();
    let rejectLogin!: (reason: unknown) => void;
    client.post.mockReturnValue(
      new Promise((_resolve, reject) => {
        rejectLogin = reject;
      }),
    );
    renderApp('/login', client);
    const visitor = userEvent.setup();
    await visitor.type(await screen.findByLabelText('Email'), 'seller@example.com');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(screen.getByRole('button', { name: 'Signing in...' })).toBeDisabled();
    rejectLogin(new ApiError('Invalid email or password.', 401));

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
  });

  it('clears the token and returns to login on logout', async () => {
    const storage = memoryStorage('stored-jwt');
    const client = fakeClient();
    client.get.mockResolvedValue({ data: seller });
    renderApp('/app/seller', client, storage);
    const visitor = userEvent.setup();

    await visitor.click(await screen.findByRole('button', { name: 'Log out' }));

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(storage.read()).toBeNull();
  });
});

function renderApp(
  path: string,
  client = fakeClient(),
  storage = memoryStorage(),
) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider client={client} storage={storage}>
        <App />
      </AuthProvider>
    </MemoryRouter>,
  );
}

function fakeClient() {
  return { get: vi.fn(), post: vi.fn() };
}

function memoryStorage(initialToken: string | null = null): TokenStorage {
  let token = initialToken;
  return {
    read: () => token,
    write: (value) => {
      token = value;
    },
    clear: () => {
      token = null;
    },
  };
}

function user(role: UserRole): AuthUser {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    email: `${role.toLowerCase()}@example.com`,
    roles: [role],
  };
}

import { render, screen, waitFor } from '@testing-library/react';
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

  it('routes an authenticated buyer from / to their role home /app/buyer', async () => {
    const storage = memoryStorage('buyer-jwt');
    const client = fakeClient();
    client.get.mockResolvedValue({ data: buyer });

    renderApp('/', client, storage);

    expect(await screen.findByRole('heading', { name: 'Buyer home' })).toBeInTheDocument();
  });

  it('routes an authenticated seller from / to their role home /app/seller', async () => {
    const storage = memoryStorage('seller-jwt');
    const client = fakeClient();
    client.get.mockResolvedValue({ data: seller });

    renderApp('/', client, storage);

    expect(await screen.findByRole('heading', { name: 'Seller home' })).toBeInTheDocument();
  });

  it('renders the public landing page on / for an anonymous visitor', async () => {
    renderApp('/');

    expect(await screen.findByRole('heading', { name: /Turn Surplus[\s\S]*Into Opportunity/i })).toBeInTheDocument();
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

  it.each(['/app/manager', '/app/manager/categories', '/app/manager/requirements', '/app/manager/requirements/r1/matches', '/app/manager/matches'])('blocks dual-role manager navigation at %s', async path => {
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

  it('switches between the sign-in and public registration forms', async () => {
    renderApp('/login');
    const visitor = userEvent.setup();

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    await visitor.click(panelAction());
    expect(await screen.findByRole('heading', { name: 'Build with less waste.' })).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());

    await visitor.click(panelAction());
    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
  });

  it('sends the supported registration DTO and signs in the new dual-role account', async () => {
    const client = fakeClient();
    const storage = memoryStorage();
    client.post.mockResolvedValue({ data: { token: 'new-account-jwt', user: { ...seller, roles: ['SELLER', 'BUYER'] } } });
    renderApp('/login', client, storage);
    const visitor = userEvent.setup();

    await visitor.click(panelAction());
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());
    await visitor.type(screen.getByLabelText('Full name'), 'Ava Builder');
    await visitor.type(screen.getByLabelText('Email'), ' ava@example.com ');
    await visitor.type(screen.getByLabelText('Phone number'), '+94771234567');
    await visitor.type(screen.getByLabelText('Business name (optional)'), 'Build Better');
    await visitor.type(screen.getByLabelText('Address'), '10 Reuse Road');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.type(screen.getByLabelText('Confirm password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Buy materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Sell materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('heading', { name: 'Seller home' })).toBeInTheDocument();
    expect(client.post).toHaveBeenCalledWith('/api/auth/register', {
      fullName: 'Ava Builder', email: 'ava@example.com', phoneNumber: '+94771234567',
      businessName: 'Build Better', address: '10 Reuse Road', password: 'Password123!', roles: ['BUYER', 'SELLER'],
    });
    expect(storage.read()).toBe('new-account-jwt');
  });

  it('rejects a mismatched confirmation before calling public registration', async () => {
    const client = fakeClient();
    renderApp('/login', client);
    const visitor = userEvent.setup();
    await visitor.click(panelAction());
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());
    await visitor.type(screen.getByLabelText('Full name'), 'Ava Builder');
    await visitor.type(screen.getByLabelText('Email'), 'ava@example.com');
    await visitor.type(screen.getByLabelText('Phone number'), '+94771234567');
    await visitor.type(screen.getByLabelText('Address'), '10 Reuse Road');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.type(screen.getByLabelText('Confirm password'), 'Different123!');
    await visitor.click(screen.getByRole('button', { name: 'Buy materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Passwords do not match.');
    expect(client.post).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: /manager/i })).not.toBeInTheDocument();
  });

  it('shows server-side registration validation errors inline', async () => {
    const client = fakeClient();
    client.post.mockRejectedValue(new ApiError('An account with that email already exists.', 409));
    renderApp('/login', client);
    const visitor = userEvent.setup();
    await visitor.click(panelAction());
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());
    await visitor.type(screen.getByLabelText('Full name'), 'Ava Builder');
    await visitor.type(screen.getByLabelText('Email'), 'ava@example.com');
    await visitor.type(screen.getByLabelText('Phone number'), '+94771234567');
    await visitor.type(screen.getByLabelText('Address'), '10 Reuse Road');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.type(screen.getByLabelText('Confirm password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Buy materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with that email already exists.');
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

function panelAction(): HTMLButtonElement {
  const action = document.querySelector<HTMLButtonElement>('.auth-panel-action');
  if (!action) throw new Error('Expected the desktop auth panel action.');
  return action;
}

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

  it('sends the supported registration DTO and requires email verification', async () => {
    const client = fakeClient();
    const storage = memoryStorage();
    client.post.mockResolvedValue({ data: { token: 'new-account-jwt', user: { ...seller, roles: ['SELLER', 'BUYER'] } } });
    renderApp('/login', client, storage);
    const visitor = userEvent.setup();

    await visitor.click(panelAction());
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());
    await visitor.type(screen.getByLabelText('Full name'), 'Ava Builder');
    await visitor.type(screen.getByLabelText('NIC'), '199912345678');
    await visitor.type(screen.getByLabelText('Email'), ' ava@example.com ');
    await visitor.type(screen.getByLabelText('Phone number'), '+94771234567');
    await visitor.type(screen.getByLabelText('Business name (optional)'), 'Build Better');
    await visitor.type(screen.getByLabelText('Address'), '10 Reuse Road');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.type(screen.getByLabelText('Confirm password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Buy materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Sell materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('heading', { name: 'Verify your email' })).toBeInTheDocument();
    expect(client.post).toHaveBeenCalledWith('/api/auth/register', {
      fullName: 'Ava Builder', email: 'ava@example.com', phoneNumber: '+94771234567', nic: '199912345678',
      businessName: 'Build Better', address: '10 Reuse Road', password: 'Password123!', roles: ['BUYER', 'SELLER'],
    });
    expect(storage.read()).toBeNull();
    expect(screen.getByLabelText('Verification code')).toBeEnabled();
    await visitor.type(screen.getByLabelText('Verification code'), '123456');
    expect(screen.getByRole('button', { name: /Verify email/ })).toBeEnabled();
    await visitor.click(screen.getByRole('button', { name: /Verify email/ }));
    expect(await screen.findByRole('heading', { name: 'Email verified successfully' })).toBeInTheDocument();
    // The success message stays visible for 1.2 seconds before returning to sign-in.
    expect(await screen.findByRole('heading', { name: 'Welcome back' }, { timeout: 2500 })).toBeInTheDocument();
    expect(storage.read()).toBeNull();
  });

  it('rejects a mismatched confirmation before calling public registration', async () => {
    const client = fakeClient();
    renderApp('/login', client);
    const visitor = userEvent.setup();
    await visitor.click(panelAction());
    await waitFor(() => expect(screen.getByLabelText('Full name')).toBeEnabled());
    await visitor.type(screen.getByLabelText('Full name'), 'Ava Builder');
    await visitor.type(screen.getByLabelText('NIC'), '199912345678');
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
    await visitor.type(screen.getByLabelText('NIC'), '199912345678');
    await visitor.type(screen.getByLabelText('Email'), 'ava@example.com');
    await visitor.type(screen.getByLabelText('Phone number'), '+94771234567');
    await visitor.type(screen.getByLabelText('Address'), '10 Reuse Road');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.type(screen.getByLabelText('Confirm password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Buy materials' }));
    await visitor.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with that email already exists.');
  });

  it('turns an unverified login result into verification and real resend actions', async () => {
    const client = fakeClient();
    const storage = memoryStorage();
    client.post.mockRejectedValueOnce(new ApiError(
      'Your email has not been verified.',
      403,
      undefined,
      'EMAIL_NOT_VERIFIED',
    ));
    renderApp('/login', client, storage);
    const visitor = userEvent.setup();

    await visitor.type(await screen.findByLabelText('Email'), 'dual@example.com');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('heading', { name: 'Your email is not verified yet.' })).toBeInTheDocument();
    expect(screen.getByText('dual@example.com')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Verify Email/ })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Resend Verification Code' })).toBeEnabled();
    expect(storage.read()).toBeNull();

    client.post.mockResolvedValueOnce({ data: { message: 'A new verification code was sent.' } });
    await visitor.click(screen.getByRole('button', { name: 'Resend Verification Code' }));
    expect(client.post).toHaveBeenLastCalledWith('/api/auth/email-verification/resend', { email: 'dual@example.com' });
    expect(await screen.findByRole('status')).toHaveTextContent(/Resend available in 0[01]:\d{2}/);

    await visitor.click(screen.getByRole('button', { name: /Verify Email/ }));
    expect(await screen.findByRole('heading', { name: 'Verify your email' })).toBeInTheDocument();
    expect(screen.getByText(/sent to dual@example.com/)).toBeInTheDocument();
  });

  it('verifies the retained login email, shows success, and returns to sign in', async () => {
    const client = fakeClient();
    client.post.mockRejectedValueOnce(new ApiError(
      'Your email has not been verified.',
      403,
      undefined,
      'EMAIL_NOT_VERIFIED',
    ));
    renderApp('/login', client);
    const visitor = userEvent.setup();

    await visitor.type(await screen.findByLabelText('Email'), 'buyer@example.com');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Sign in' }));
    await visitor.click(await screen.findByRole('button', { name: /Verify Email/ }));
    await visitor.type(await screen.findByLabelText('Verification code'), '123456');

    client.post.mockResolvedValueOnce({ data: { message: 'Your email has been verified.' } });
    await visitor.click(screen.getByRole('button', { name: /Verify email/ }));
    expect(await screen.findByRole('heading', { name: 'Email verified successfully' })).toBeInTheDocument();
    expect(client.post).toHaveBeenLastCalledWith('/api/auth/email-verification/verify', { email: 'buyer@example.com', code: '123456' });

    expect(await screen.findByRole('heading', { name: 'Welcome back' }, { timeout: 2500 })).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toHaveValue('buyer@example.com');
  });

  it('does not fake resend success when email delivery fails', async () => {
    const client = fakeClient();
    client.post.mockRejectedValueOnce(new ApiError(
      'Your email has not been verified.',
      403,
      undefined,
      'EMAIL_NOT_VERIFIED',
    ));
    renderApp('/login', client);
    const visitor = userEvent.setup();

    await visitor.type(await screen.findByLabelText('Email'), 'seller@example.com');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Sign in' }));
    client.post.mockRejectedValueOnce(new ApiError(
      'We could not send the email. Please try again shortly.',
      503,
      undefined,
      'EMAIL_DELIVERY_FAILED',
    ));
    await visitor.click(await screen.findByRole('button', { name: 'Resend Verification Code' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('We could not send the email. Please try again shortly.');
    expect(screen.queryByText(/Resend available in/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Resend Verification Code' })).toBeEnabled();
  });

  it('keeps verification open with a friendly wrong-code error', async () => {
    const client = fakeClient();
    client.post.mockRejectedValueOnce(new ApiError(
      'Your email has not been verified.',
      403,
      undefined,
      'EMAIL_NOT_VERIFIED',
    ));
    renderApp('/login', client);
    const visitor = userEvent.setup();

    await visitor.type(await screen.findByLabelText('Email'), 'seller@example.com');
    await visitor.type(screen.getByLabelText('Password'), 'Password123!');
    await visitor.click(screen.getByRole('button', { name: 'Sign in' }));
    await visitor.click(await screen.findByRole('button', { name: /Verify Email/ }));
    await visitor.type(await screen.findByLabelText('Verification code'), '654321');
    client.post.mockRejectedValueOnce(new ApiError('The code is invalid. Please try again.', 400, undefined, 'EMAIL_VERIFICATION_CODE_INVALID'));
    await visitor.click(screen.getByRole('button', { name: /Verify email/ }));

    expect(await screen.findByRole('alert')).toHaveTextContent('The code is invalid. Please try again.');
    expect(screen.getByLabelText('Verification code')).toHaveValue('654321');
  });

  it('clears the token and returns to login on logout', async () => {
    const storage = memoryStorage('stored-jwt');
    const client = fakeClient();
    client.get.mockResolvedValue({ data: seller });
    renderApp('/app/seller', client, storage);
    const visitor = userEvent.setup();

    await visitor.click(await screen.findByRole('button', { name: 'Log out' }));
    await visitor.click(screen.getByRole('button', { name: 'Log Out' }));

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


it('recovers a password with editable numeric OTP, paste, backspace, Enter and API retry', async () => {
  const client = fakeClient();
  client.post.mockResolvedValue({ data: {} });
  renderApp('/login', client);
  const visitor = userEvent.setup();
  await visitor.click(await screen.findByRole('button', { name: 'Forgot password?' }));
  await visitor.type(screen.getByLabelText('Email'), 'buyer@example.com');
  await visitor.click(screen.getByRole('button', { name: /Send reset code/ }));
  const code = await screen.findByLabelText('Reset code');
  expect(code).toBeEnabled();
  await visitor.type(code, '12a34x56');
  expect(code).toHaveValue('123456');
  await visitor.keyboard('{Backspace}');
  expect(code).toHaveValue('12345');
  await visitor.clear(code);
  await visitor.paste('123456');
  expect(code).toHaveValue('123456');
  await visitor.type(screen.getByLabelText('New password'), 'Password123!');
  await visitor.type(screen.getByLabelText('Confirm new password'), 'Password123!');
  expect(screen.getByRole('button', { name: /Reset password/ })).toBeEnabled();
  client.post.mockRejectedValueOnce(new ApiError('Code expired. Try again.', 400));
  await visitor.keyboard('{Enter}');
  expect(await screen.findByRole('alert')).toHaveTextContent('Code expired');
  expect(code).toBeEnabled();
  await visitor.clear(code);
  await visitor.type(code, '654321');
  await visitor.click(screen.getByRole('button', { name: /Reset password/ }));
  expect(client.post).toHaveBeenLastCalledWith('/api/auth/reset-password', { email: 'buyer@example.com', code: '654321', newPassword: 'Password123!' });
  expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
});

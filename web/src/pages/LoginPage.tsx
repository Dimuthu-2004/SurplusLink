import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { login, pending, error, clearError } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearError();
    const trimmedEmail = email.trim();
    if (!trimmedEmail || !trimmedEmail.includes('@')) {
      setValidationError('Enter a valid email address.');
      return;
    }
    if (!password) {
      setValidationError('Enter your password.');
      return;
    }
    setValidationError(null);
    if (await login(trimmedEmail, password)) {
      const from = (location.state as { from?: unknown } | null)?.from;
      navigate(typeof from === 'string' ? from : '/app', { replace: true });
    }
  }

  return (
    <main className="auth-page">
      <aside className="auth-intro" aria-hidden="true">
        <span className="auth-intro-mark">+</span>
        <p>THE SMARTER MATERIAL MARKETPLACE</p>
        <h2>Bring every useful material closer to its next purpose.</h2>
        <div><b>AI-assisted matching</b><span>Human-controlled decisions</span></div>
      </aside>
      <section className="auth-card" aria-labelledby="login-title">
        <div className="auth-brand" aria-hidden="true">+</div>
        <p className="eyebrow">SurplusLink</p>
        <h1 id="login-title">Welcome back</h1>
        <p className="muted">Sign in to continue to your workspace.</p>

        {(validationError || error) && (
          <div className="error-message" role="alert">
            {validationError || error}
          </div>
        )}

        <form onSubmit={submit} noValidate>
          <label htmlFor="email">Email</label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={pending}
          />
          <label htmlFor="password">Password</label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            disabled={pending}
          />
          <button className="button button-primary" type="submit" disabled={pending}>
            {pending ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  );
}

import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { PublicRegistration, UserRole } from '../auth/authTypes';
import './loginPage.css';

type AuthMode = 'signIn' | 'signUp';
type AuthStep = AuthMode | 'verify' | 'forgot' | 'reset';
type MarketplaceRole = Extract<UserRole, 'SELLER' | 'BUYER'>;

interface RegistrationForm extends PublicRegistration {
  confirmPassword: string;
}

const emptyRegistration: RegistrationForm = {
  fullName: '', email: '', nic: '', phoneNumber: '', businessName: '', address: '', password: '', confirmPassword: '', roles: [],
};

export function LoginPage() {
  const { login, register, verifyEmail, resendVerification, forgotPassword, resetPassword, pending, error, clearError } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [mode, setMode] = useState<AuthMode>('signIn');
  const [step, setStep] = useState<AuthStep>('signIn');
  const [panelMode, setPanelMode] = useState<AuthMode>('signIn');
  const [changing, setChanging] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [loginForm, setLoginForm] = useState({ email: '', password: '' });
  const [verification, setVerification] = useState({ email: '', code: '' });
  const [reset, setReset] = useState({ email: '', code: '', password: '', confirmPassword: '' });
  const [resendSeconds, setResendSeconds] = useState(0);
  const [registration, setRegistration] = useState<RegistrationForm>(emptyRegistration);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [showLoginPassword, setShowLoginPassword] = useState(false);
  const [showRegistrationPassword, setShowRegistrationPassword] = useState(false);
  const transitionTimer = useRef<number | undefined>(undefined);
  const formTimer = useRef<number | undefined>(undefined);

  useEffect(() => () => {
    window.clearTimeout(transitionTimer.current);
    window.clearTimeout(formTimer.current);
  }, []);
  useEffect(() => {
    if (!resendSeconds) return;
    const timer = window.setInterval(() => setResendSeconds(value => Math.max(0, value - 1)), 1000);
    return () => window.clearInterval(timer);
  }, [resendSeconds]);

  function switchMode(nextMode: AuthMode) {
    if (nextMode === mode || changing || pending) return;
    window.clearTimeout(transitionTimer.current);
    window.clearTimeout(formTimer.current);
    clearError();
    setValidationError(null);
    setChanging(true);
    setLeaving(true);
    setPanelMode(nextMode);
    formTimer.current = window.setTimeout(() => {
      setMode(nextMode);
      setLeaving(false);
    }, 120);
    transitionTimer.current = window.setTimeout(() => setChanging(false), 650);
  }

  function destination() {
    const from = (location.state as { from?: unknown } | null)?.from;
    return typeof from === 'string' ? from : '/app';
  }

  async function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearError();
    const email = loginForm.email.trim();
    if (!isEmail(email)) return setValidationError('Enter a valid email address.');
    if (!loginForm.password) return setValidationError('Enter your password.');
    setValidationError(null);
    if (await login(email, loginForm.password)) navigate(destination(), { replace: true });
  }

  async function submitRegistration(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearError();
    const errorMessage = registrationError(registration);
    if (errorMessage) return setValidationError(errorMessage);
    setValidationError(null);
    const { confirmPassword: _confirmPassword, ...request } = registration;
    if (await register(request)) { setVerification({ email: request.email.trim(), code: '' }); setStep('verify'); setResendSeconds(60); }
  }

  async function submitVerification(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (await verifyEmail(verification.email, verification.code)) { setStep('signIn'); setLoginForm({ email: verification.email, password: '' }); } }
  async function resendCode() { if (resendSeconds || !verification.email) return; if (await resendVerification(verification.email)) setResendSeconds(60); }
  async function submitForgot(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!isEmail(reset.email)) return setValidationError('Enter a valid email address.'); if (await forgotPassword(reset.email)) { setStep('reset'); setResendSeconds(60); setValidationError('If an account exists for this email, a reset code has been sent.'); } }
  async function submitReset(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!/^\d{6}$/.test(reset.code)) return setValidationError('Enter the six-digit code.'); if (reset.password.length < 8) return setValidationError('Your password must be at least 8 characters.'); if (reset.password !== reset.confirmPassword) return setValidationError('Passwords do not match.'); if (await resetPassword(reset.email, reset.code, reset.password)) { setStep('signIn'); setLoginForm({ email: reset.email, password: '' }); } }

  function updateRegistration<K extends keyof RegistrationForm>(key: K, value: RegistrationForm[K]) {
    setRegistration(current => ({ ...current, [key]: value }));
  }

  function toggleRole(role: MarketplaceRole) {
    updateRegistration('roles', registration.roles.includes(role)
      ? registration.roles.filter(current => current !== role)
      : [...registration.roles, role]);
  }

  const message = validationError || error;
  const signUp = mode === 'signUp';

  return (
    <main className={`auth-experience auth-mode-${panelMode} ${changing ? 'is-changing' : ''} ${leaving ? 'is-leaving' : ''}`}>
      <div className="auth-background" aria-hidden="true" />
      <div className="auth-vignette" aria-hidden="true" />
      <section className="auth-shell" aria-label="SurplusLink account access">
        <div className="auth-form-surface">
          <Brand dark />
          <div className="auth-form-stage" aria-busy={changing || pending}>
            {step === 'verify' ? (
              <form className="auth-form" onSubmit={submitVerification}><p className="auth-kicker">EMAIL CONFIRMATION</p><h1>Verify your email</h1><p className="auth-subtitle">Enter the six-digit code sent to {verification.email}.</p><FormMessage message={message} /><div className="auth-fields"><Field label="Verification code" htmlFor="verificationCode"><input id="verificationCode" inputMode="numeric" autoComplete="one-time-code" maxLength={6} value={verification.code} onChange={event => setVerification(current => ({ ...current, code: event.target.value.replace(/\D/g, '') }))} /></Field></div><button className="auth-submit" type="submit" disabled={pending || verification.code.length !== 6}>Verify email <Arrow /></button><button className="auth-panel-action" type="button" onClick={resendCode} disabled={pending || resendSeconds > 0}>{resendSeconds ? `Resend code in 00:${String(resendSeconds).padStart(2, '0')}` : 'Resend code'}</button><p className="auth-mobile-switch"><button type="button" onClick={() => setStep('signIn')}>Back to sign in</button></p></form>
            ) : step === 'forgot' ? (
              <form className="auth-form" onSubmit={submitForgot}><p className="auth-kicker">ACCOUNT RECOVERY</p><h1>Reset your password</h1><p className="auth-subtitle">We will send a six-digit reset code if an account exists.</p><FormMessage message={message} /><div className="auth-fields"><Field label="Email" htmlFor="forgotEmail"><input id="forgotEmail" type="email" value={reset.email} onChange={event => setReset(current => ({ ...current, email: event.target.value }))} /></Field></div><button className="auth-submit" type="submit" disabled={pending}>Send reset code <Arrow /></button><p className="auth-mobile-switch"><button type="button" onClick={() => setStep('signIn')}>Back to sign in</button></p></form>
            ) : step === 'reset' ? (
              <form className="auth-form" onSubmit={submitReset}><p className="auth-kicker">ACCOUNT RECOVERY</p><h1>Choose a new password</h1><FormMessage message={message} /><div className="auth-fields"><Field label="Reset code" htmlFor="resetCode"><input id="resetCode" inputMode="numeric" autoComplete="one-time-code" maxLength={6} value={reset.code} onChange={event => setReset(current => ({ ...current, code: event.target.value.replace(/\D/g, '') }))} /></Field><PasswordField id="newPassword" label="New password" autoComplete="new-password" value={reset.password} onChange={value => setReset(current => ({ ...current, password: value }))} visible={showRegistrationPassword} onToggle={() => setShowRegistrationPassword(value => !value)} disabled={pending} /><Field label="Confirm new password" htmlFor="confirmNewPassword"><input id="confirmNewPassword" type="password" value={reset.confirmPassword} onChange={event => setReset(current => ({ ...current, confirmPassword: event.target.value }))} /></Field></div><button className="auth-submit" type="submit" disabled={pending}>Reset password <Arrow /></button><button className="auth-panel-action" type="button" disabled={pending || resendSeconds > 0} onClick={async () => { if (await forgotPassword(reset.email)) setResendSeconds(60); }}>{resendSeconds ? `Resend code in 00:${String(resendSeconds).padStart(2, '0')}` : 'Resend code'}</button></form>
            ) : !signUp ? (
              <form className="auth-form auth-sign-in-form" onSubmit={submitLogin} noValidate aria-labelledby="login-title">
                <p className="auth-kicker">YOUR MARKETPLACE</p><h1 id="login-title">Welcome back</h1>
                <p className="auth-subtitle">Sign in to manage materials and keep projects moving.</p><FormMessage message={message} />
                <div className="auth-fields">
                  <Field label="Email" htmlFor="email"><input id="email" name="email" type="email" autoComplete="email" value={loginForm.email} onChange={event => setLoginForm(current => ({ ...current, email: event.target.value }))} disabled={pending || changing} /></Field>
                  <PasswordField id="password" label="Password" autoComplete="current-password" value={loginForm.password} onChange={value => setLoginForm(current => ({ ...current, password: value }))} visible={showLoginPassword} onToggle={() => setShowLoginPassword(visible => !visible)} disabled={pending || changing} />
                </div>
                <button className="auth-submit" type="submit" disabled={pending || changing}>{pending ? <><Spinner /> Signing in...</> : <>Sign in <Arrow /></>}</button>
                {error === 'Your email has not been verified.' && <p className="auth-mobile-switch"><button type="button" onClick={() => { setVerification({ email: loginForm.email.trim(), code: '' }); setStep('verify'); }}>Verify Email</button><button type="button" onClick={async () => { const email = loginForm.email.trim(); setVerification({ email, code: '' }); if (await resendVerification(email)) setResendSeconds(60); }}>Resend Code</button></p>}
                <p className="auth-mobile-switch"><button type="button" onClick={() => { clearError(); setStep('forgot'); }}>Forgot password?</button></p>
                <p className="auth-mobile-switch">New to SurplusLink? <button type="button" onClick={() => switchMode('signUp')} disabled={pending || changing}>Create account</button></p>
              </form>
            ) : (
              <form className="auth-form auth-sign-up-form" onSubmit={submitRegistration} noValidate aria-labelledby="register-title">
                <p className="auth-kicker">JOIN THE EXCHANGE</p><h1 id="register-title">Build with less waste.</h1>
                <p className="auth-subtitle">Create your marketplace account in a few details.</p><FormMessage message={message} />
                <div className="auth-fields auth-registration-fields">
                  <Field label="Full name" htmlFor="fullName"><input id="fullName" name="fullName" autoComplete="name" value={registration.fullName} onChange={event => updateRegistration('fullName', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="Email" htmlFor="registerEmail"><input id="registerEmail" name="email" type="email" autoComplete="email" value={registration.email} onChange={event => updateRegistration('email', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="Sri Lankan NIC" htmlFor="nic"><input id="nic" name="nic" value={registration.nic} onChange={event => updateRegistration('nic', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="Phone number" htmlFor="phoneNumber"><input id="phoneNumber" name="phoneNumber" type="tel" autoComplete="tel" value={registration.phoneNumber} onChange={event => updateRegistration('phoneNumber', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="Business name (optional)" htmlFor="businessName"><input id="businessName" name="businessName" autoComplete="organization" value={registration.businessName} onChange={event => updateRegistration('businessName', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="Address" htmlFor="address" full><input id="address" name="address" autoComplete="street-address" value={registration.address} onChange={event => updateRegistration('address', event.target.value)} disabled={pending || changing} /></Field>
                  <PasswordField id="registerPassword" label="Password" autoComplete="new-password" value={registration.password} onChange={value => updateRegistration('password', value)} visible={showRegistrationPassword} onToggle={() => setShowRegistrationPassword(visible => !visible)} disabled={pending || changing} />
                  <Field label="Confirm password" htmlFor="confirmPassword"><input id="confirmPassword" name="confirmPassword" type={showRegistrationPassword ? 'text' : 'password'} autoComplete="new-password" value={registration.confirmPassword} onChange={event => updateRegistration('confirmPassword', event.target.value)} disabled={pending || changing} /></Field>
                </div>
                <PasswordStrength password={registration.password} />
                <fieldset className="auth-role-picker" disabled={pending || changing}><legend>I want to</legend><div><button type="button" className={registration.roles.includes('BUYER') ? 'is-selected' : ''} aria-pressed={registration.roles.includes('BUYER')} onClick={() => toggleRole('BUYER')}>Buy materials</button><button type="button" className={registration.roles.includes('SELLER') ? 'is-selected' : ''} aria-pressed={registration.roles.includes('SELLER')} onClick={() => toggleRole('SELLER')}>Sell materials</button></div><small>Choose one or both marketplace roles. Manager access is assigned separately.</small></fieldset>
                <button className="auth-submit" type="submit" disabled={pending || changing}>{pending ? <><Spinner /> Creating account...</> : <>Create account <Arrow /></>}</button>
                <p className="auth-mobile-switch">Already have an account? <button type="button" onClick={() => switchMode('signIn')} disabled={pending || changing}>Sign in</button></p>
              </form>
            )}
          </div>
        </div>
        <aside className="auth-brand-panel" aria-label="SurplusLink introduction"><div className="auth-panel-texture" aria-hidden="true" /><div className="auth-panel-content"><Brand /><div className="auth-panel-copy"><p className="auth-panel-kicker">SURPLUS, CONNECTED</p><h2>{signUp ? 'Already part of the exchange?' : 'New to SurplusLink?'}</h2><p>{signUp ? 'Sign in to return to your materials, opportunities, and project activity.' : 'Create your account to list surplus materials, source what your project needs, and keep useful materials in circulation.'}</p></div><button className="auth-panel-action" type="button" onClick={() => switchMode(signUp ? 'signIn' : 'signUp')} disabled={pending || changing}>{signUp ? 'Sign in' : 'Create account'} <Arrow /></button><div className="auth-panel-line" aria-hidden="true"><span /><span /><span /></div></div></aside>
      </section>
    </main>
  );
}

function Brand({ dark = false }: { dark?: boolean }) { return <div className={`auth-logo ${dark ? 'auth-logo-dark' : ''}`}><img src="/images/brand/surpluslink-mark.png" alt="SurplusLink" /><span>SurplusLink</span></div>; }
function Field({ label, htmlFor, children, full = false }: { label: string; htmlFor: string; children: ReactNode; full?: boolean }) { return <label className={`auth-field ${full ? 'auth-field-full' : ''}`} htmlFor={htmlFor}><span>{label}</span>{children}</label>; }
function PasswordField({ id, label, autoComplete, value, onChange, visible, onToggle, disabled }: { id: string; label: string; autoComplete: string; value: string; onChange(value: string): void; visible: boolean; onToggle(): void; disabled: boolean }) { return <label className="auth-field" htmlFor={id}><span>{label}</span><span className="auth-password-input"><input id={id} name={id} type={visible ? 'text' : 'password'} autoComplete={autoComplete} value={value} onChange={event => onChange(event.target.value)} disabled={disabled} /><button type="button" aria-label={visible ? 'Hide password' : 'Show password'} aria-pressed={visible} onClick={onToggle} disabled={disabled}>{visible ? 'Hide' : 'Show'}</button></span></label>; }
function FormMessage({ message }: { message: string | null }) { return message ? <div className="auth-error" role="alert">{message}</div> : null; }
function PasswordStrength({ password }: { password: string }) { const score = Number(password.length >= 8) + Number(/[A-Z]/.test(password)) + Number(/[0-9]/.test(password)) + Number(/[^A-Za-z0-9]/.test(password)); return <div className="auth-password-strength" aria-live="polite"><div aria-hidden="true">{[1, 2, 3, 4].map(level => <span key={level} className={score >= level ? 'is-active' : ''} />)}</div><span>{password ? (score >= 4 ? 'Strong password' : score >= 2 ? 'Keep strengthening it' : 'Use 8+ characters, a number and symbol') : 'Use 8+ characters, a number and symbol'}</span></div>; }
function Arrow() { return <svg aria-hidden="true" viewBox="0 0 20 20" fill="none"><path d="M3 10h13m-5-5 5 5-5 5" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" /></svg>; }
function Spinner() { return <span className="auth-spinner" aria-hidden="true" />; }
function isEmail(value: string) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value); }
function registrationError(form: RegistrationForm) { if (!form.fullName.trim()) return 'Enter your full name.'; if (!isEmail(form.email.trim())) return 'Enter a valid email address.'; if (!/^\d{9}[VvXx]$|^\d{12}$/.test(form.nic.trim().replace(/\s/g, ''))) return 'Enter a valid Sri Lankan NIC number.'; if (!/^(?:0?94|\+94|0)7\d{8}$/.test(form.phoneNumber.trim().replace(/[ -]/g, ''))) return 'Enter a valid Sri Lankan phone number.'; if (!form.address.trim()) return 'Enter your address.'; if (form.password.length < 8) return 'Your password must be at least 8 characters.'; if (form.password !== form.confirmPassword) return 'Passwords do not match.'; if (!form.roles.length) return 'Choose how you want to use SurplusLink.'; return null; }

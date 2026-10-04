import { OtpInput } from '../components/OtpInput';
import React, { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import Lottie from 'lottie-react';
import emailLottieData from '../assets/animations/email.json';
import { useAuth } from '../auth/AuthContext';
import type { PublicRegistration, UserRole } from '../auth/authTypes';
import { SurplusLinkLogo } from '../components/SurplusLinkLogo';
import { StatusAnimation } from '../components/StatusAnimation';
import './loginPage.css';

type AuthMode = 'signIn' | 'signUp';
type AuthStep = AuthMode | 'unverified' | 'verify' | 'forgot' | 'reset';
type MarketplaceRole = Extract<UserRole, 'SELLER' | 'BUYER'>;

interface RegistrationForm extends PublicRegistration {
  confirmPassword: string;
}

const emptyRegistration: RegistrationForm = {
  fullName: '', email: '', nic: '', phoneNumber: '', businessName: '', address: '', password: '', confirmPassword: '', roles: [],
};

export function LoginPage() {
  const { login, register, verifyEmail, resendVerification, forgotPassword, resetPassword, pending, error, errorCode, clearError } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const initialMode: AuthMode = location.pathname === '/register' ? 'signUp' : 'signIn';
  const [mode, setMode] = useState<AuthMode>(initialMode);
  const [step, setStep] = useState<AuthStep>(initialMode);
  const [panelMode, setPanelMode] = useState<AuthMode>(initialMode);
  const [changing, setChanging] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [loginForm, setLoginForm] = useState({ email: '', password: '' });
  const [verification, setVerification] = useState({ email: '', code: '' });
  const [reset, setReset] = useState({ email: '', code: '', password: '', confirmPassword: '' });
  const [resendSeconds, setResendSeconds] = useState(0);
  const [verificationSucceeded, setVerificationSucceeded] = useState(false);
  const [registration, setRegistration] = useState<RegistrationForm>(emptyRegistration);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [showLoginPassword, setShowLoginPassword] = useState(false);
  const [showRegistrationPassword, setShowRegistrationPassword] = useState(false);
  const transitionTimer = useRef<number | undefined>(undefined);
  const formTimer = useRef<number | undefined>(undefined);
  const verificationTimer = useRef<number | undefined>(undefined);

  useEffect(() => () => {
    window.clearTimeout(transitionTimer.current);
    window.clearTimeout(formTimer.current);
    window.clearTimeout(verificationTimer.current);
  }, []);
  useEffect(() => {
    if (!resendSeconds) return;
    const timer = window.setInterval(() => setResendSeconds(value => Math.max(0, value - 1)), 1000);
    return () => window.clearInterval(timer);
  }, [resendSeconds]);
  useEffect(() => {
    if (changing) return;
    const routeMode: AuthMode = location.pathname === '/register' ? 'signUp' : 'signIn';
    if (routeMode === mode || !['signIn', 'signUp'].includes(step)) return;
    setMode(routeMode);
    setStep(routeMode);
    setPanelMode(routeMode);
    setLeaving(false);
  }, [changing, location.pathname, mode, step]);

  function switchMode(nextMode: AuthMode) {
    if (nextMode === mode || changing || pending) return;
    window.clearTimeout(transitionTimer.current);
    window.clearTimeout(formTimer.current);
    clearError();
    setValidationError(null);
    setChanging(true);
    setLeaving(true);
    setPanelMode(nextMode);
    const formDelay = import.meta.env.MODE === 'test' ? 0 : 120;
    const transDelay = import.meta.env.MODE === 'test' ? 0 : 650;
    formTimer.current = window.setTimeout(() => {
      setMode(nextMode);
      setStep(nextMode);
      setLeaving(false);
      navigate(nextMode === 'signUp' ? '/register' : '/login', { replace: true });
    }, formDelay);
    transitionTimer.current = window.setTimeout(() => setChanging(false), transDelay);
  }

  function destination() {
    const from = (location.state as { from?: unknown } | null)?.from;
    return typeof from === 'string' ? from : '/app';
  }

  async function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearError();
    const email = loginForm.email.trim();
    if (!email) return setValidationError('Enter your email address.');
    if (!isEmail(email)) return setValidationError('Enter a valid email address.');
    if (!loginForm.password) return setValidationError('Enter your password.');
    setValidationError(null);
    const result = await login(email, loginForm.password);
    if (result === 'authenticated') navigate(destination(), { replace: true });
    if (result === 'emailNotVerified') {
      setVerification({ email, code: '' });
      setResendSeconds(0);
      setVerificationSucceeded(false);
      setStep('unverified');
    }
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

  async function submitVerification(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await verifyEmail(verification.email, verification.code)) {
      setVerificationSucceeded(true);
      setLoginForm(current => ({ ...current, email: verification.email }));
      verificationTimer.current = window.setTimeout(() => {
        setMode('signIn');
        setPanelMode('signIn');
        setStep('signIn');
        setVerificationSucceeded(false);
      }, 1200);
    }
  }
  async function resendCode() { if (resendSeconds || !verification.email) return; if (await resendVerification(verification.email)) setResendSeconds(60); }
  async function submitForgot(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!isEmail(reset.email)) return setValidationError('Enter a valid email address.'); if (await forgotPassword(reset.email)) { setStep('reset'); setResendSeconds(60); setValidationError('If an account exists for this email, a reset code has been sent.'); } }
  async function submitReset(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!/^\d{6}$/.test(reset.code)) return setValidationError('Enter the six-digit code.'); if (reset.password.length < 8) return setValidationError('Your password must be at least 8 characters.'); if (reset.password !== reset.confirmPassword) return setValidationError('Passwords do not match.'); setValidationError(null); clearError(); if (await resetPassword(reset.email, reset.code, reset.password)) { setStep('signIn'); setLoginForm({ email: reset.email, password: '' }); } }

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
            {step === 'unverified' ? (
              <section className="auth-form auth-verification-panel" aria-labelledby="unverified-title">
                <VerificationIcon state="attention" />
                <p className="auth-kicker">EMAIL VERIFICATION</p>
                <h1 id="unverified-title">Your email is not verified yet.</h1>
                <p className="auth-subtitle">Verify your email to continue using SurplusLink.</p>
                <p className="auth-verification-email">{verification.email}</p>
                <FormMessage message={errorCode && errorCode !== 'EMAIL_NOT_VERIFIED' ? error : null} />
                <div className="auth-verification-actions">
                  <button className="auth-submit" type="button" onClick={() => { clearError(); setStep('verify'); }}>Verify Email <Arrow /></button>
                  <button className="auth-secondary-action" type="button" onClick={resendCode} disabled={pending || resendSeconds > 0}>
                    {pending ? <><Spinner /> Sending...</> : 'Resend Verification Code'}
                  </button>
                </div>
                {resendSeconds > 0 && <p className="auth-countdown" role="status">Resend available in {formatCountdown(resendSeconds)}</p>}
              </section>
            ) : step === 'verify' ? (
              verificationSucceeded ? (
                <section className="auth-form auth-verification-success" role="status">
                  <VerificationIcon state="success" />
                  <p className="auth-kicker">EMAIL CONFIRMED</p>
                  <h1>Email verified successfully</h1>
                  <p className="auth-subtitle">Returning you to Sign In...</p>
                </section>
              ) : (
                <form className="auth-form" onSubmit={submitVerification}>
                  <VerificationIcon state="code" />
                  <p className="auth-kicker">EMAIL CONFIRMATION</p><h1>Verify your email</h1><p className="auth-subtitle">Enter the six-digit code sent to {verification.email}.</p>
                  <FormMessage message={message} />
                  <div className="auth-fields"><Field label="Verification code" htmlFor="verificationCode"><OtpInput id="verificationCode" value={verification.code} disabled={pending} onChange={code => setVerification(current => ({ ...current, code }))} /></Field></div>
                  <button className="auth-submit" type="submit" disabled={pending || verification.code.length !== 6}>Verify email <Arrow /></button>
                  <button className="auth-secondary-action" type="button" onClick={resendCode} disabled={pending || resendSeconds > 0}>Resend Verification Code</button>
                  {resendSeconds > 0 && <p className="auth-countdown" role="status">Resend available in {formatCountdown(resendSeconds)}</p>}
                  <p className="auth-back-link"><button type="button" onClick={() => { clearError(); setStep('signIn'); setMode('signIn'); setPanelMode('signIn'); }}>Back to sign in</button></p>
                </form>
              )
            ) : step === 'forgot' ? (
              <form className="auth-form" onSubmit={submitForgot}><p className="auth-kicker">ACCOUNT RECOVERY</p><h1>Reset your password</h1><p className="auth-subtitle">We will send a six-digit reset code if an account exists.</p><FormMessage message={message} /><div className="auth-fields"><Field label="Email" htmlFor="forgotEmail"><input id="forgotEmail" type="email" value={reset.email} onChange={event => setReset(current => ({ ...current, email: event.target.value }))} /></Field></div><button className="auth-submit" type="submit" disabled={pending}>Send reset code <Arrow /></button><p className="auth-mobile-switch"><button type="button" onClick={() => { setStep('signIn'); setMode('signIn'); setPanelMode('signIn'); }}>Back to sign in</button></p></form>
            ) : step === 'reset' ? (
              <form className="auth-form" onSubmit={submitReset}><p className="auth-kicker">ACCOUNT RECOVERY</p><h1>Choose a new password</h1><FormMessage message={message} /><div className="auth-fields"><Field label="Reset code" htmlFor="resetCode"><OtpInput id="resetCode" value={reset.code} disabled={pending} onChange={code => setReset(current => ({ ...current, code }))} /></Field><PasswordField id="newPassword" label="New password" autoComplete="new-password" value={reset.password} onChange={value => setReset(current => ({ ...current, password: value }))} visible={showRegistrationPassword} onToggle={() => setShowRegistrationPassword(value => !value)} disabled={pending} /><Field label="Confirm new password" htmlFor="confirmNewPassword"><input id="confirmNewPassword" type="password" value={reset.confirmPassword} onChange={event => setReset(current => ({ ...current, confirmPassword: event.target.value }))} /></Field></div><button className="auth-submit" type="submit" disabled={pending || !/^\d{6}$/.test(reset.code) || reset.password.length < 8 || reset.password !== reset.confirmPassword}>Reset password <Arrow /></button><button className="auth-panel-action" type="button" disabled={pending || resendSeconds > 0} onClick={async () => { if (await forgotPassword(reset.email)) setResendSeconds(60); }}>{resendSeconds ? `Resend code in 00:${String(resendSeconds).padStart(2, '0')}` : 'Resend code'}</button></form>
            ) : !signUp ? (
              <form className="auth-form auth-sign-in-form" onSubmit={submitLogin} noValidate aria-labelledby="login-title">
                <p className="auth-kicker">YOUR MARKETPLACE</p><h1 id="login-title">Welcome back</h1>
                <p className="auth-subtitle">Sign in to manage materials and keep projects moving.</p><FormMessage message={message} />
                <div className="auth-fields">
                  <Field label="Email" htmlFor="email"><input id="email" name="email" type="email" autoComplete="email" value={loginForm.email} onChange={event => setLoginForm(current => ({ ...current, email: event.target.value }))} disabled={pending || changing} /></Field>
                  <PasswordField id="password" label="Password" autoComplete="current-password" value={loginForm.password} onChange={value => setLoginForm(current => ({ ...current, password: value }))} visible={showLoginPassword} onToggle={() => setShowLoginPassword(visible => !visible)} disabled={pending || changing} />
                </div>
                <button className="auth-submit" type="submit" disabled={pending || changing}>{pending ? <><Spinner /> Signing in...</> : <>Sign in <Arrow /></>}</button>
                <p className="auth-forgot-password"><button type="button" onClick={() => { clearError(); setStep('forgot'); }}>Forgot password?</button></p>
                <p className="auth-mobile-switch">New to SurplusLink? <button type="button" onClick={() => switchMode('signUp')} disabled={pending || changing}>Create account</button></p>
              </form>
            ) : (
              <form className="auth-form auth-sign-up-form" onSubmit={submitRegistration} noValidate aria-labelledby="register-title">
                <p className="auth-kicker">JOIN THE EXCHANGE</p><h1 id="register-title">Build with less waste.</h1>
                <p className="auth-subtitle">Create your marketplace account in a few details.</p><FormMessage message={message} />
                <div className="auth-fields auth-registration-fields">
                  <Field label="Full name" htmlFor="fullName"><input id="fullName" name="fullName" autoComplete="name" value={registration.fullName} onChange={event => updateRegistration('fullName', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="Email" htmlFor="registerEmail"><input id="registerEmail" name="email" type="email" autoComplete="email" value={registration.email} onChange={event => updateRegistration('email', event.target.value)} disabled={pending || changing} /></Field>
                  <Field label="NIC" htmlFor="nic"><input id="nic" name="nic" value={registration.nic} onChange={event => updateRegistration('nic', event.target.value)} disabled={pending || changing} /></Field>
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
        <aside className="auth-brand-panel" aria-label="SurplusLink introduction"><div className="auth-panel-texture" aria-hidden="true" /><div className="auth-panel-content"><Brand /><div className={`auth-login-ready ${changing || leaving || pending || !['signIn', 'signUp'].includes(step) ? 'is-hidden' : ''}`} aria-hidden={changing || leaving || pending}><StatusAnimation kind="login" size={220} loop label="Ready to use SurplusLink" /></div><div className="auth-panel-copy"><p className="auth-panel-kicker">SURPLUS, CONNECTED</p><h2>{signUp ? 'Already part of the exchange?' : 'New to SurplusLink?'}</h2><p>{signUp ? 'Sign in to return to your materials, opportunities, and project activity.' : 'Create your account to list surplus materials, source what your project needs, and keep useful materials in circulation.'}</p></div><button className="auth-panel-action" type="button" onClick={() => switchMode(signUp ? 'signIn' : 'signUp')} disabled={pending || changing}>{signUp ? 'Sign in' : 'Create account'} <Arrow /></button><div className="auth-panel-line" aria-hidden="true"><span /><span /><span /></div></div></aside>
      </section>
    </main>
  );
}

function Brand({ dark = false }: { dark?: boolean }) { return <div className={`auth-logo ${dark ? 'auth-logo-dark' : ''}`}><SurplusLinkLogo className="auth-logo-image" /></div>; }
function Field({ label, htmlFor, children, full = false }: { label: string; htmlFor: string; children: ReactNode; full?: boolean }) { return <label className={`auth-field ${full ? 'auth-field-full' : ''}`} htmlFor={htmlFor}><span>{label}</span>{children}</label>; }
function PasswordField({ id, label, autoComplete, value, onChange, visible, onToggle, disabled }: { id: string; label: string; autoComplete: string; value: string; onChange(value: string): void; visible: boolean; onToggle(): void; disabled: boolean }) { return <label className="auth-field" htmlFor={id}><span>{label}</span><span className="auth-password-input"><input id={id} name={id} type={visible ? 'text' : 'password'} autoComplete={autoComplete} value={value} onChange={event => onChange(event.target.value)} disabled={disabled} /><button type="button" aria-label={visible ? 'Hide password' : 'Show password'} aria-pressed={visible} onClick={onToggle} disabled={disabled}>{visible ? 'Hide' : 'Show'}</button></span></label>; }
function FormMessage({ message }: { message: string | null }) { return message ? <div className="auth-error" role="alert">{message}</div> : null; }
function PasswordStrength({ password }: { password: string }) {
  const hasLength = password.length >= 8;
  const hasUpper = /[A-Z]/.test(password);
  const hasLower = /[a-z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasSpecial = /[^A-Za-z0-9]/.test(password);
  const score = Number(hasLength) + Number(hasUpper && hasLower) + Number(hasNumber) + Number(hasSpecial);
  const isStrong = hasLength && hasUpper && hasLower && hasNumber && hasSpecial;

  return (
    <div className="auth-password-strength" aria-live="polite">
      <div aria-hidden="true">
        {[1, 2, 3, 4].map(level => <span key={level} className={score >= level ? 'is-active' : ''} />)}
      </div>
      <span>
        {!password
          ? 'Must be 8+ chars with uppercase, lowercase, number & symbol'
          : isStrong
          ? '✓ Strong password'
          : 'Include 8+ chars, uppercase, lowercase, number & special character'}
      </span>
    </div>
  );
}

function Arrow() { return <svg aria-hidden="true" viewBox="0 0 20 20" fill="none"><path d="M3 10h13m-5-5 5 5-5 5" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" /></svg>; }
function Spinner() { return <span className="auth-spinner" aria-hidden="true" />; }

interface SafeLottieProps {
  animationData: unknown;
  loop?: boolean;
  style?: React.CSSProperties;
  fallback: ReactNode;
}

interface SafeLottieState {
  hasError: boolean;
}

class SafeLottie extends React.Component<SafeLottieProps, SafeLottieState> {
  constructor(props: SafeLottieProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): SafeLottieState {
    return { hasError: true };
  }

  componentDidCatch() {
    // Safe error containment
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback;
    }
    return (
      <Lottie
        animationData={this.props.animationData}
        loop={this.props.loop ?? true}
        style={this.props.style}
        onError={() => this.setState({ hasError: true })}
      />
    );
  }
}

function VerificationIcon({ state }: { state: 'attention' | 'code' | 'success' }) {
  if (state === 'success') {
    return (
      <span className="auth-verification-icon is-success" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none"><path d="m6.5 12 3.4 3.4L17.8 8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>
      </span>
    );
  }

  const fallbackSvg = (
    <span className={`auth-verification-icon is-${state}`} aria-hidden="true">
      <svg viewBox="0 0 24 24" fill="none">
        <path d="M4.5 7.5 12 13l7.5-5.5M5 6h14v12H5z" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="18.5" cy="17.5" r="3" stroke="currentColor" strokeWidth="1.65" />
        <path d="M18.5 16v2" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" />
      </svg>
    </span>
  );

  return (
    <div className="auth-verification-lottie-container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100%', margin: '0 auto 0.75rem' }}>
      <SafeLottie
        animationData={emailLottieData}
        loop={true}
        style={{ width: 130, height: 130 }}
        fallback={fallbackSvg}
      />
    </div>
  );
}

function formatCountdown(seconds: number) { return `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`; }
function isEmail(value: string) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value); }

function isStrongPassword(password: string): boolean {
  return (
    password.length >= 8 &&
    /[A-Z]/.test(password) &&
    /[a-z]/.test(password) &&
    /[0-9]/.test(password) &&
    /[^A-Za-z0-9]/.test(password)
  );
}

function registrationError(form: RegistrationForm) {
  if (!form.fullName.trim()) return 'Enter your full name.';
  if (!form.email.trim()) return 'Enter your email address.';
  if (!isEmail(form.email.trim())) return 'Enter a valid email address.';
  if (!/^\d{9}[VvXx]$|^\d{12}$/.test(form.nic.trim().replace(/\s/g, ''))) return 'Enter a valid NIC number.';
  if (!/^(?:0?94|\+94|0)7\d{8}$/.test(form.phoneNumber.trim().replace(/[ -]/g, ''))) return 'Enter a valid Sri Lankan phone number.';
  if (!form.address.trim()) return 'Enter your address.';
  if (!form.password) return 'Enter a password.';
  if (form.password.length < 8) return 'Your password must be at least 8 characters.';
  if (!isStrongPassword(form.password)) return 'Password must include uppercase, lowercase, a number, and a special character.';
  if (form.password !== form.confirmPassword) return 'Passwords do not match.';
  if (!form.roles.length) return 'Choose how you want to use SurplusLink.';
  return null;
}

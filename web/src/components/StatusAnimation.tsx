import { Component, lazy, Suspense, useEffect, useState, type ErrorInfo, type ReactNode } from 'react';
import thumbsUp from '../assets/animations/thumbs-up-birdie.json';
import smartphone from '../assets/animations/smartphone-call.json';
import loginReady from '../assets/animations/login-ready.json';
import paymentDone from '../assets/animations/payment-done.json';

const animations = { approval: thumbsUp, phone: smartphone, login: loginReady, completed: paymentDone } as const;
const Lottie = lazy(() => import('lottie-react'));

class OptionalAnimationBoundary extends Component<{ children: ReactNode; fallback: ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.warn('Optional Lottie animation could not render.', error, info);
  }

  render() {
    return this.state.failed ? this.props.fallback : this.props.children;
  }
}

/** Keeps an acknowledgement visible while the optional renderer loads or fails. */
function ApprovalFallback() {
  return <div className="status-animation-fallback" data-testid="status-animation-fallback" data-approval-animation="true" aria-hidden="true">
    <span>🐦</span><strong>✓</strong>
  </div>;
}

function GenericFallback() {
  return <div className="status-animation-fallback" data-testid="status-animation-fallback" aria-hidden="true"><span>&#x21BB;</span></div>;
}

function isAnimationData(value: unknown): value is Record<string, unknown> {
  if (!value || typeof value !== 'object') return false;
  const data = value as Record<string, unknown>;
  return Array.isArray(data.layers) && typeof data.w === 'number' && typeof data.h === 'number';
}

export function StatusAnimation({ kind, size = 200, loop = false, label, decorative = false, className = '', onComplete }: {
  kind: keyof typeof animations; size?: number; loop?: boolean; label: string; decorative?: boolean; className?: string; onComplete?: () => void;
}) {
  const [reduced, setReduced] = useState(false);
  const animationData = animations[kind] as unknown;
  useEffect(() => {
    const query = window.matchMedia?.('(prefers-reduced-motion: reduce)');
    if (!query) return;
    const update = () => setReduced(query.matches);
    update(); query.addEventListener?.('change', update);
    return () => query.removeEventListener?.('change', update);
  }, []);
  if (!isAnimationData(animationData)) return null;
  const fallback = kind === 'approval' ? <ApprovalFallback /> : <GenericFallback />;
  return <div className={`status-animation ${className}`} style={{ width: size, maxWidth: '100%', aspectRatio: '1' }} {...(decorative ? { 'aria-hidden': true } : { role: 'img', 'aria-label': label })}>
    {reduced ? fallback :
    <OptionalAnimationBoundary fallback={fallback}>
      <Suspense fallback={fallback}>
        <Lottie animationData={animationData} loop={loop} autoplay onComplete={onComplete} />
      </Suspense>
    </OptionalAnimationBoundary>}
  </div>;
}

export function SuccessOverlay({ kind, title, message, onComplete }: {
  kind: 'approval' | 'completed'; title: string; message: string; onComplete: () => void;
}) {
  useEffect(() => {
    const timer = window.setTimeout(onComplete, 3200);
    return () => window.clearTimeout(timer);
  }, [onComplete]);
  return <div className="success-overlay" role="dialog" aria-modal="true" aria-live="polite">
    <div className="success-overlay-card">
      <StatusAnimation kind={kind} size={170} loop label={title} decorative onComplete={onComplete} />
      <h2>{title}</h2><p>{message}</p>
    </div>
  </div>;
}

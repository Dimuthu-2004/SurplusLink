import { useEffect, useState } from 'react';
import Lottie from 'lottie-react';
import submittedAnimation from '../assets/animations/submitted.json';
import workflowRunningAnimation from '../assets/animations/workflow-running.json';
import './submissionAnimationOverlays.css';

function useReducedMotion() {
  const [reducedMotion, setReducedMotion] = useState(() =>
    window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false,
  );

  useEffect(() => {
    const query = window.matchMedia?.('(prefers-reduced-motion: reduce)');
    if (!query) return undefined;
    const update = () => setReducedMotion(query.matches);
    query.addEventListener('change', update);
    return () => query.removeEventListener('change', update);
  }, []);

  return reducedMotion;
}

type OverlayText = { title: string; message?: string };

export function SubmittedAnimationOverlay({
  open,
  title,
  message,
  onCompleted,
}: OverlayText & { open: boolean; onCompleted: () => void }) {
  const reducedMotion = useReducedMotion();

  useEffect(() => {
    if (!open || !reducedMotion) return undefined;
    const timer = window.setTimeout(onCompleted, 700);
    return () => window.clearTimeout(timer);
  }, [onCompleted, open, reducedMotion]);

  if (!open) return null;

  return (
    <div className="submission-animation-backdrop" role="dialog" aria-modal="true" aria-live="polite" aria-label={`${title}${message ? `. ${message}` : ''}`}>
      <section className="submission-animation-card">
        {reducedMotion ? (
          <span className="submission-animation-static-icon" aria-hidden="true">✓</span>
        ) : (
          <Lottie
            animationData={submittedAnimation}
            loop={false}
            autoplay
            className="submission-animation-art"
            onComplete={onCompleted}
          />
        )}
        <h2>{title}</h2>
        {message && <p>{message}</p>}
      </section>
    </div>
  );
}

/** Render only while the backend reports that the workflow remains active. */
export function WorkflowRunningOverlay({
  open,
  title,
  message,
}: OverlayText & { open: boolean }) {
  const reducedMotion = useReducedMotion();
  if (!open) return null;

  return (
    <div className="submission-animation-backdrop" role="dialog" aria-modal="true" aria-live="polite" aria-label={`${title}. ${message}`}>
      <section className="submission-animation-card">
        {reducedMotion ? (
          <span className="submission-animation-static-icon" aria-hidden="true">⋯</span>
        ) : (
          <Lottie
            animationData={workflowRunningAnimation}
            loop
            autoplay
            className="submission-animation-art"
          />
        )}
        <h2>{title}</h2>
        <p>{message}</p>
      </section>
    </div>
  );
}

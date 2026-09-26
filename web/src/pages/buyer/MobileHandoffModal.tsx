import React, { useEffect, useState, useCallback, useRef } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { createMobileHandoff, type MobileHandoffCreationResponse } from '../../api/buyerMarketplaceApi';
import { SurplusLinkLogo } from '../../components/SurplusLinkLogo';

export interface MobileHandoffModalProps {
  isOpen: boolean;
  onClose: () => void;
  categoryId: string;
  categoryName: string;
}

export function MobileHandoffModal({
  isOpen,
  onClose,
  categoryId,
  categoryName,
}: MobileHandoffModalProps) {
  const [handoff, setHandoff] = useState<MobileHandoffCreationResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [secondsRemaining, setSecondsRemaining] = useState<number>(0);
  const [showIntroduction, setShowIntroduction] = useState(true);
  const timerRef = useRef<number | null>(null);

  const initHandoff = useCallback(async () => {
    if (!categoryId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await createMobileHandoff(categoryId, 'REACT_MARKETPLACE');
      setHandoff(res);
      const expires = new Date(res.expiresAt).getTime();
      const now = Date.now();
      const remaining = Math.max(0, Math.floor((expires - now) / 1000));
      setSecondsRemaining(remaining);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Unable to generate mobile handoff.');
    } finally {
      setLoading(false);
    }
  }, [categoryId]);

  useEffect(() => {
    if (isOpen) {
      setShowIntroduction(true);
    } else {
      setHandoff(null);
      setError(null);
      setSecondsRemaining(0);
      if (timerRef.current) {
        window.clearInterval(timerRef.current);
        timerRef.current = null;
      }
    }
  }, [isOpen]);

  useEffect(() => {
    if (isOpen && !showIntroduction && !handoff && !loading && !error) {
      initHandoff();
    }
  }, [error, handoff, initHandoff, isOpen, loading, showIntroduction]);

  useEffect(() => {
    if (!handoff || secondsRemaining <= 0) return;

    timerRef.current = window.setInterval(() => {
      setSecondsRemaining((prev) => {
        if (prev <= 1) {
          if (timerRef.current) window.clearInterval(timerRef.current);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      if (timerRef.current) {
        window.clearInterval(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [handoff]);

  if (!isOpen) return null;

  const isExpired = secondsRemaining <= 0 && handoff !== null;
  const minutes = Math.floor(secondsRemaining / 60);
  const seconds = secondsRemaining % 60;
  const formattedCountdown = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

  return (
    <div
      className="handoff-modal-backdrop"
      role="dialog"
      aria-modal="true"
      aria-labelledby="handoff-modal-title"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="handoff-modal-dialog">
        <button
          type="button"
          className="handoff-modal-close"
          aria-label="Close handoff modal"
          onClick={onClose}
        >
          &times;
        </button>

        {showIntroduction ? (
          <section className="handoff-introduction" aria-labelledby="handoff-modal-title">
            <SurplusLinkLogo className="handoff-intro-logo" />
            <h2 id="handoff-modal-title" className="ai-mobile-heading">
              Find the best match with SurplusLink Mobile
            </h2>
            <p className="ai-mobile-desc">
              You are viewing materials in the <strong>{categoryName}</strong> category. Use SurplusLink Mobile to create your actual requirement and let the AI compare available materials based on quantity, budget, location and logistics.
            </p>
            <ol className="handoff-intro-steps">
              <li>Install or open SurplusLink Mobile</li>
              <li>Sign in with the same account</li>
              <li>Choose <strong>Scan Web Handoff QR</strong></li>
              <li>Scan the QR code</li>
            </ol>
            <div className="handoff-intro-actions">
              <button type="button" className="button button-secondary" onClick={onClose}>Cancel</button>
              <button type="button" className="button button-primary" onClick={() => setShowIntroduction(false)}>Continue to QR</button>
            </div>
          </section>
        ) : <>
        <div className="ai-mobile-badge" style={{ justifyContent: 'center' }}>
          <span>AI Matching Handoff</span>
        </div>

        <h2 id="handoff-modal-title" className="ai-mobile-heading" style={{ fontSize: '1.4rem' }}>
          Continue in SurplusLink Mobile
        </h2>

        <p className="ai-mobile-desc" style={{ marginBottom: '0.75rem' }}>
          Open SurplusLink Mobile and choose <strong>&lsquo;Scan Web Handoff QR&rsquo;</strong>.
        </p>

        <div style={{
          marginBottom: '1.25rem',
          padding: '0.5rem 0.85rem',
          backgroundColor: '#fff7ed',
          borderRadius: '8px',
          border: '1px solid #fed7aa',
          fontSize: '0.88rem',
          color: '#c2410c'
        }}>
          Selected Category: <strong>{categoryName}</strong>
        </div>

        {loading && (
          <div style={{ padding: '3rem 0', color: 'var(--sl-market-text-muted)' }}>
            <div className="spinner" style={{ margin: '0 auto 1rem' }} />
            <p>Generating secure handoff...</p>
          </div>
        )}

        {error && (
          <div style={{ padding: '1.5rem 0' }}>
            <p className="field-error" style={{ marginBottom: '1rem' }}>
              {error}
            </p>
            <button
              type="button"
              className="button button-primary"
              onClick={initHandoff}
            >
              Try Again
            </button>
          </div>
        )}

        {!loading && !error && handoff && (
          <>
            <div className="handoff-qr-wrapper">
              {isExpired ? (
                <div
                  style={{
                    width: 200,
                    height: 200,
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: '#dc2626',
                    padding: '1rem',
                  }}
                >
                  <p style={{ fontWeight: 700, margin: '0 0 0.5rem 0' }}>Code Expired</p>
                  <p style={{ fontSize: '0.8rem', color: '#64748b', margin: 0 }}>
                    Please regenerate a new code to continue.
                  </p>
                </div>
              ) : (
                <QRCodeSVG
                  value={`SLH1:${handoff.code}`}
                  size={200}
                  level="M"
                  marginSize={1}
                  fgColor="#1e293b"
                  bgColor="#ffffff"
                />
              )}
            </div>

            <p className={`handoff-expiry-timer ${isExpired ? 'warning' : ''}`}>
              {isExpired
                ? 'This handoff has expired.'
                : `This handoff expires in ${formattedCountdown}`}
            </p>

            <p style={{
              fontSize: '0.82rem',
              color: '#64748b',
              textAlign: 'center',
              marginTop: '0.25rem',
              marginBottom: '1rem'
            }}>
              Sign in with the same SurplusLink account to continue.
            </p>

            <div className="handoff-actions-col">
              {!isExpired ? (
                <a
                  href={handoff.deepLink}
                  className="handoff-deep-link-btn"
                  target="_self"
                  rel="noopener noreferrer"
                >
                  <svg
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <rect x="5" y="2" width="14" height="20" rx="2" ry="2" />
                    <line x1="12" y1="18" x2="12.01" y2="18" />
                  </svg>
                  Open in Mobile App
                </a>
              ) : (
                <button
                  type="button"
                  className="button button-primary"
                  onClick={initHandoff}
                >
                  Regenerate QR Code
                </button>
              )}

              <button
                type="button"
                className="button button-secondary"
                onClick={onClose}
              >
                Close
              </button>
            </div>
          </>
        )}
        </>}
      </div>
    </div>
  );
}

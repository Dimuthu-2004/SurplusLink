import React, { useState } from 'react';
import Lottie from 'lottie-react';
import chatbotAnimationData from '../assets/animations/male call center.json';

interface AiChatbotFabProps {
  onClick?: () => void;
  className?: string;
}

export function AiChatbotFab({ onClick, className = '' }: AiChatbotFabProps) {
  const [hasError, setHasError] = useState(false);

  return (
    <button
      type="button"
      className={`ai-chatbot-fab ${className}`}
      onClick={onClick}
      aria-label="AI Assistant"
      title="SurplusLink AI Assistant"
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '0.5rem',
        backgroundColor: '#f59e0b',
        color: '#0f172a',
        border: 'none',
        borderRadius: '9999px',
        padding: '0.5rem 1rem',
        fontWeight: 700,
        fontSize: '0.875rem',
        cursor: 'pointer',
        boxShadow: '0 4px 14px rgba(245, 158, 11, 0.35)',
        transition: 'transform 0.2s ease, box-shadow 0.2s ease',
      }}
    >
      <div style={{ width: 28, height: 28, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        {!hasError ? (
          <Lottie
            animationData={chatbotAnimationData}
            loop={true}
            style={{ width: 28, height: 28 }}
            onError={() => setHasError(true)}
          />
        ) : (
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <rect x="3" y="11" width="18" height="10" rx="2" />
            <circle cx="12" cy="5" r="2" />
            <path d="M12 7v4" />
            <line x1="8" y1="16" x2="8" y2="16" />
            <line x1="16" y1="16" x2="16" y2="16" />
          </svg>
        )}
      </div>
      <span>AI Assistant</span>
    </button>
  );
}

import { useEffect, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { useNavigate } from 'react-router-dom';
import './logoutConfirmation.css';

export function LogoutConfirmation({ onLogout, className = 'button button-secondary logout-button', children = 'Log out', onOpen }: { onLogout(): void; className?: string; children?: ReactNode; onOpen?(): void }) {
  const [open, setOpen] = useState(false);
  const cancelRef = useRef<HTMLButtonElement>(null);
  const navigate = useNavigate();
  useEffect(() => {
    if (!open) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    cancelRef.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => { if (event.key === 'Escape') setOpen(false); };
    document.addEventListener('keydown', onKeyDown);
    return () => { document.body.style.overflow = previousOverflow; document.removeEventListener('keydown', onKeyDown); };
  }, [open]);
  function confirm() { setOpen(false); onLogout(); navigate('/login', { replace: true }); }
  return <><button className={className} type="button" onClick={() => { onOpen?.(); setOpen(true); }}>{children}</button>{open && createPortal(<div className="logout-modal-backdrop" role="presentation" onMouseDown={() => setOpen(false)}><section className="logout-modal" role="dialog" aria-modal="true" aria-labelledby="logout-title" aria-describedby="logout-description" onMouseDown={event => event.stopPropagation()}><h2 id="logout-title">Log out of SurplusLink?</h2><p id="logout-description">Are you sure you want to end this session?</p><div className="logout-modal-actions"><button ref={cancelRef} className="button button-secondary" type="button" onClick={() => setOpen(false)}>Cancel</button><button className="button button-danger" type="button" onClick={confirm}>Log Out</button></div></section></div>, document.body)}</>;
}

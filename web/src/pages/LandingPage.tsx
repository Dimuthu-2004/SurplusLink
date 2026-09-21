import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { roleHomePath } from '../routing/roleRoutes';

const features = [
  ['◈', 'Surplus material listings', 'Capture reusable material quantity, condition, images and location in one clear listing.'],
  ['⌁', 'Buyer requirements', 'Publish what a project needs, including quantities, budgets, deadlines and location.'],
  ['✦', 'AI-powered matching', 'Discover promising candidates with an AI-assisted matching workflow.'],
  ['↗', 'Logistics insights', 'Bring distance, duration and transport estimates into the decision.'],
  ['✓', 'Controlled approval', 'Important recommendations remain subject to authorised human review.'],
  ['◌', 'Tracking & history', 'Keep workflow status and audit history visible as work moves forward.'],
];

function Reveal({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`reveal ${className}`}>{children}</div>;
}

export function LandingPage() {
  const { status, user } = useAuth();
  const [open, setOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const authenticated = status === 'authenticated' && user;
  const accountPath = user ? roleHomePath(user.roles) : '/login';

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12);
    const observer = new IntersectionObserver((entries) => entries.forEach(entry => entry.isIntersecting && entry.target.classList.add('is-visible')), { threshold: 0.12 });
    document.querySelectorAll('.reveal').forEach(node => observer.observe(node));
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => { observer.disconnect(); window.removeEventListener('scroll', onScroll); };
  }, []);

  const closeMenu = () => setOpen(false);
  return <main className="landing-page">
    <header className={`public-nav ${scrolled ? 'public-nav-scrolled' : ''}`}>
      <a className="public-brand" href="#top" onClick={closeMenu}><span>S</span>SurplusLink</a>
      <button className="menu-toggle" aria-label="Toggle navigation menu" aria-expanded={open} onClick={() => setOpen(!open)}><i /><i /><i /></button>
      <nav className={open ? 'open' : ''} aria-label="Public navigation">
        {['Home', 'Features', 'How It Works', 'AI Matching', 'Sustainability'].map((label) => <a key={label} href={`#${label.toLowerCase().replaceAll(' ', '-')}`} onClick={closeMenu}>{label}</a>)}
        <Link className="nav-login" to={accountPath} onClick={closeMenu}>{authenticated ? 'Open Dashboard' : 'Login'}</Link>
      </nav>
    </header>

    <section id="top" className="hero section-shell">
      <div className="hero-copy">
        <p className="pill">✦ AI-powered construction material marketplace</p>
        <h1>Turn surplus materials<br />into <em>new opportunities.</em></h1>
        <p className="hero-intro">SurplusLink connects reusable construction materials with projects that need them — with intelligent matching, logistics insights and controlled human approval.</p>
        <div className="hero-actions"><a className="cta primary" href="#features">Explore SurplusLink <b>→</b></a><Link className="cta secondary" to={accountPath}>{authenticated ? 'Open Dashboard' : 'Login'}</Link></div>
        <div className="hero-proof"><span>✓ Smarter material reuse</span><span>✓ AI-assisted matching</span><span>✓ Logistics insights</span></div>
      </div>
      <div className="hero-visual" aria-label="Illustrative material matching workflow">
        <div className="orb orb-one" /><div className="orb orb-two" />
        <div className="market-window"><div className="window-bar"><i /><i /><i /><span>Material match overview</span></div><div className="window-body"><div className="listing-card"><span className="tile-art">▦</span><div><small>MATERIAL AVAILABLE</small><strong>Floor Tiles</strong><b>500 m² · Verified</b></div></div><div className="match-line"><span>AI Match</span><b>92%</b></div><div className="buyer-card"><small>BUYER REQUIREMENT</small><strong>400 m²</strong><span>Project finish · Colombo</span></div></div></div>
        <div className="float-card route">↗ <span><b>12.4 km</b>Estimated route</span></div><div className="float-card verified">✓ Verified listing</div><div className="float-card recommendation">✦ AI recommended</div>
      </div>
    </section>

    <section className="value-strip section-shell" aria-label="Platform value"><span>♻ <b>Reduce</b> waste</span><span>◈ <b>Save</b> costs</span><span>✦ <b>Smart</b> matching</span><span>↗ <b>Route</b> insights</span><span>✓ <b>Human</b> controlled</span></section>

    <section id="features" className="section-shell section">
      <Reveal><p className="section-kicker">The connected marketplace</p><h2>Everything needed for smarter <em>material reuse.</em></h2><p className="section-lead">A clear, role-aware workflow for moving useful construction materials toward their next purpose.</p></Reveal>
      <div className="bento-grid">{features.map(([icon, title, copy], index) => <Reveal className={`feature-card feature-${index}`} key={title}><span className="feature-icon">{icon}</span><h3>{title}</h3><p>{copy}</p>{index < 3 && <span className="feature-mini" aria-hidden="true">{index === 0 ? '500 m²' : index === 1 ? 'Need: 400 m²' : 'Find candidates →'}</span>}</Reveal>)}</div>
    </section>

    <section id="how-it-works" className="section-shell section workflow-section"><Reveal><p className="section-kicker">A considered path forward</p><h2>How SurplusLink <em>works.</em></h2></Reveal><div className="workflow">{[['01','List or request','Make available materials or project needs visible.'],['02','AI finds candidates','Surface relevant possibilities.'],['03','Logistics evaluates','Understand distance and transport.'],['04','Manager reviews','Keep decisions in human hands.'],['05','Material is reserved','Move forward after approval.']].map(([n, title, copy]) => <Reveal className="workflow-step" key={n}><span>{n}</span><h3>{title}</h3><p>{copy}</p></Reveal>)}</div></section>

    <section id="ai-matching" className="section-shell ai-section section"><Reveal><div><p className="section-kicker">AI that supports judgment</p><h2>Better decisions, with people <em>still in control.</em></h2><p className="section-lead">SurplusLink helps teams identify and assess potential material matches. High-impact reservations require authorised human approval.</p><a className="text-link" href="#how-it-works">See the workflow <b>→</b></a></div></Reveal><Reveal className="ai-flow"><div>Requirement</div><i>↓</i><div>Planner</div><i>↓</i><div className="active">Material matching <b>AI-assisted</b></div><i>↓</i><div>Logistics & validation</div><i>↓</i><div>Human approval <span>✓</span></div></Reveal></section>

    <section className="section-shell section"><Reveal><p className="section-kicker">Illustrative marketplace</p><h2>Useful materials, ready to be <em>rediscovered.</em></h2></Reveal><div className="market-grid"><Reveal className="material-card tiles"><span>Finishes</span><div className="material-visual">▦</div><h3>Floor Tiles</h3><p>500 m² · LKR 800 / m²</p><b>● Verified · Colombo</b></Reveal><Reveal className="material-card steel"><span>Structure</span><div className="material-visual">▤</div><h3>Steel Bars</h3><p>1,200 kg · Available</p><b>● Verified · Kandy</b></Reveal><Reveal className="material-card timber"><span>Timber</span><div className="material-visual">▥</div><h3>Reclaimed Timber</h3><p>350 units · Available</p><b>● Verified · Galle</b></Reveal></div><p className="demo-note">Illustrative interface preview — not live marketplace data.</p></section>

    <section id="sustainability" className="sustainability"><div className="section-shell"><Reveal><p className="section-kicker">A more resourceful built environment</p><h2>Build more. <em>Waste less.</em></h2><p className="section-lead">SurplusLink helps redirect usable construction materials away from waste streams and toward projects that can reuse them.</p></Reveal><div className="impact-grid"><Reveal><div><b>♻</b><h3>Reuse materials</h3></div></Reveal><Reveal><div><b>◌</b><h3>Reduce waste</h3></div></Reveal><Reveal><div><b>◈</b><h3>Reduce procurement cost</h3></div></Reveal><Reveal><div><b>⌁</b><h3>Improve resource visibility</h3></div></Reveal></div></div></section>

    <section className="section-shell capability-strip" aria-label="Platform capabilities"><Reveal><div><strong>AI</strong><span>Assisted matching</span></div></Reveal><Reveal><div><strong>5-step</strong><span>Decision workflow</span></div></Reveal><Reveal><div><strong>Role-based</strong><span>Marketplace access</span></div></Reveal><Reveal><div><strong>Human</strong><span>Approval control</span></div></Reveal></section>

    <section className="section-shell section roles"><Reveal><p className="section-kicker">Flexible by design</p><h2>One account. <em>Multiple possibilities.</em></h2></Reveal><div className="role-grid">{[['SELL','List surplus construction materials and connect them with interested buyers.'],['BUY','Publish project requirements and discover suitable available materials.'],['BOTH','Buy and sell using the same SurplusLink account.']].map(([title, text]) => <Reveal key={title}><article><span>{title === 'BOTH' ? '↔' : title === 'SELL' ? '↑' : '↓'}</span><h3>{title}</h3><p>{text}</p></article></Reveal>)}</div></section>

    <section className="section-shell security"><Reveal><div><b>Secure authentication</b><span>Role-based access</span><span>Human approval</span><span>Auditable workflow</span></div></Reveal></section>
    <section className="section-shell final-cta"><Reveal><div><p className="section-kicker">Make each material count</p><h2>Give surplus materials a <em>second purpose.</em></h2><p>Explore a smarter way to connect available construction materials with real project requirements.</p><div className="hero-actions"><Link className="cta primary" to={accountPath}>{authenticated ? 'Open Dashboard' : 'Login'} <b>→</b></Link><a className="cta secondary" href="#how-it-works">Explore how it works</a></div></div></Reveal></section>
    <footer className="public-footer"><div className="section-shell"><div><a className="public-brand" href="#top"><span>S</span>SurplusLink</a><p>AI-assisted construction material reuse marketplace.</p></div><div><b>Explore</b><a href="#features">Features</a><a href="#how-it-works">How it works</a><a href="#ai-matching">AI matching</a></div><div><b>Account</b><Link to={accountPath}>{authenticated ? 'Open dashboard' : 'Login'}</Link></div><div><b>Project technology</b><span>ASP.NET Core · React</span><span>Flutter · PostgreSQL · Agentic AI</span></div></div></footer>
  </main>;
}

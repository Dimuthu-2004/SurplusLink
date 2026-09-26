import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { roleHomePath } from '../routing/roleRoutes';
import './landingPage.css';

const heroImages = [
  '/images/hero/warehouse-timber.png',
  '/images/hero/reclamation-yard.png',
  '/images/hero/tiles-and-timber.png',
  '/images/hero/steel-loading-bay.png',
];

type IconName = 
  | 'layers' 
  | 'search' 
  | 'route' 
  | 'check' 
  | 'recycle' 
  | 'shield' 
  | 'arrow' 
  | 'box' 
  | 'clock' 
  | 'file-text' 
  | 'menu' 
  | 'close';

function Icon({ name }: { name: IconName }) {
  const paths: Record<IconName, ReactNode> = {
    layers: (
      <>
        <path d="m12 3 8 4.5-8 4.5-8-4.5L12 3Z" />
        <path d="m4 12 8 4.5 8-4.5M4 16.5 12 21l8-4.5" />
      </>
    ),
    search: (
      <>
        <circle cx="10.5" cy="10.5" r="6.5" />
        <path d="m16 16 4 4" />
      </>
    ),
    route: (
      <>
        <circle cx="6" cy="18" r="2" />
        <circle cx="18" cy="6" r="2" />
        <path d="M8 18c7 0 1-8 8-10" />
      </>
    ),
    check: (
      <>
        <circle cx="12" cy="12" r="9" />
        <path d="m8 12 2.5 2.5L16 9" />
      </>
    ),
    recycle: (
      <>
        <path d="m7 7 2-3 2 3M9 4v7m6 6-2 3-2-3m2 3v-7M5 14l-2-3 3-2m-3 2h7m9 1 2 3-3 2m3-2h-7" />
      </>
    ),
    shield: (
      <>
        <path d="M12 3 20 6v5c0 5-3.4 8.3-8 10-4.6-1.7-8-5-8-10V6l8-3Z" />
        <path d="m8.5 12 2.2 2.2 4.8-5" />
      </>
    ),
    arrow: <path d="M5 12h13m-5-5 5 5-5 5" />,
    box: (
      <>
        <path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z" />
        <path d="m3.3 7 8.7 5 8.7-5M12 22V12" />
      </>
    ),
    clock: (
      <>
        <circle cx="12" cy="12" r="9" />
        <path d="M12 6v6l4 2" />
      </>
    ),
    'file-text': (
      <>
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" />
      </>
    ),
    menu: (
      <>
        <line x1="4" x2="20" y1="12" y2="12" />
        <line x1="4" x2="20" y1="6" y2="6" />
        <line x1="4" x2="20" y1="18" y2="18" />
      </>
    ),
    close: (
      <>
        <line x1="18" x2="6" y1="6" y2="18" />
        <line x1="6" x2="18" y1="6" y2="18" />
      </>
    ),
  };

  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {paths[name]}
    </svg>
  );
}

function Reveal({
  children,
  className = '',
}: {
  children: ReactNode;
  className?: string;
}) {
  return <div className={`lp-reveal ${className}`}>{children}</div>;
}

export function LandingPage() {
  const { status, user } = useAuth();
  const [open, setOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const [activeSlide, setActiveSlide] = useState(0);

  const authenticated = status === 'authenticated' && user;
  const accountPath = user ? roleHomePath(user.roles) : '/login';

  // Smooth scroll handler ensuring ZERO dead links
  function handleSmoothScroll(e: React.MouseEvent<HTMLAnchorElement>, targetId: string) {
    e.preventDefault();
    const el = document.getElementById(targetId);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth' });
      window.history.pushState(null, '', `#${targetId}`);
    }
    setOpen(false);
  }

  // Header scroll detection & scroll reveals (safe in JSDOM / non-browser)
  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 20);
    window.addEventListener('scroll', onScroll, { passive: true });

    if (typeof IntersectionObserver === 'undefined') {
      return () => window.removeEventListener('scroll', onScroll);
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
          }
        });
      },
      { threshold: 0.12 }
    );

    document.querySelectorAll('.lp-reveal').forEach((node) => observer.observe(node));

    return () => {
      observer.disconnect();
      window.removeEventListener('scroll', onScroll);
    };
  }, []);

  // Hero slideshow timer respecting prefers-reduced-motion
  useEffect(() => {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return;
    const timer = window.setInterval(() => {
      setActiveSlide((current) => (current + 1) % heroImages.length);
    }, 6000);
    return () => window.clearInterval(timer);
  }, []);

  return (
    <div className="lp-wrapper">
      {/* ====================================================================
          1. NAVIGATION
          ==================================================================== */}
      <header className={`lp-nav ${scrolled ? 'lp-nav-scrolled' : ''}`}>
        <div className="lp-container">
          <a 
            className="lp-brand" 
            href="#top" 
            onClick={(e) => handleSmoothScroll(e, 'top')}
          >
            <img 
              className="lp-brand-img" 
              src="/images/brand/surpluslink-mark.png" 
              alt="SurplusLink" 
            />
            <span>SurplusLink</span>
          </a>

          <button
            className="lp-menu-btn"
            type="button"
            aria-label={open ? 'Close navigation menu' : 'Open navigation menu'}
            aria-expanded={open}
            onClick={() => setOpen((prev) => !prev)}
          >
            <Icon name={open ? 'close' : 'menu'} />
          </button>

          <nav className={`lp-nav-links ${open ? 'open' : ''}`} aria-label="Public navigation">
            <a 
              className="lp-nav-link" 
              href="#top" 
              onClick={(e) => handleSmoothScroll(e, 'top')}
            >
              Home
            </a>
            <a 
              className="lp-nav-link" 
              href="#platform" 
              onClick={(e) => handleSmoothScroll(e, 'platform')}
            >
              Platform
            </a>
            <a 
              className="lp-nav-link" 
              href="#how-it-works" 
              onClick={(e) => handleSmoothScroll(e, 'how-it-works')}
            >
              How it works
            </a>
            <a 
              className="lp-nav-link" 
              href="#features" 
              onClick={(e) => handleSmoothScroll(e, 'features')}
            >
              Features
            </a>
            <a 
              className="lp-nav-link" 
              href="#materials" 
              onClick={(e) => handleSmoothScroll(e, 'materials')}
            >
              Materials
            </a>
            <a 
              className="lp-nav-link" 
              href="#impact" 
              onClick={(e) => handleSmoothScroll(e, 'impact')}
            >
              Impact
            </a>
            <Link 
              className="lp-nav-cta" 
              to={accountPath} 
              onClick={() => setOpen(false)}
            >
              {authenticated ? 'Open Dashboard' : 'Login'} <Icon name="arrow" />
            </Link>
          </nav>
        </div>
      </header>

      {/* ====================================================================
          SECTION 1: HERO
          ==================================================================== */}
      <section id="top" className="lp-hero">
        <div className="lp-hero-slides" aria-hidden="true">
          {heroImages.map((image, index) => (
            <img
              key={image}
              src={image}
              className={`lp-hero-slide ${index === activeSlide ? 'active' : ''}`}
              alt=""
            />
          ))}
        </div>
        <div className="lp-hero-overlay" />

        <div className="lp-container">
          <div className="lp-hero-content">
            <p className="lp-hero-eyebrow">CONSTRUCTION MATERIALS, PUT BACK TO WORK</p>
            <h1 className="lp-hero-title">
              Turn Surplus<br />
              <span className="lp-accent">Into Opportunity.</span>
            </h1>

            <div className="lp-hero-actions">
              <Link className="lp-btn-primary" to={accountPath}>
                {authenticated ? 'Open Dashboard' : 'Login'} <Icon name="arrow" />
              </Link>
              <a 
                className="lp-btn-secondary" 
                href="#platform" 
                onClick={(e) => handleSmoothScroll(e, 'platform')}
              >
                Explore the platform
              </a>
            </div>

            <div className="lp-hero-tags">
              <span className="lp-hero-tag">
                <Icon name="layers" /> List useful materials
              </span>
              <span className="lp-hero-tag">
                <Icon name="search" /> Source for a project
              </span>
              <span className="lp-hero-tag">
                <Icon name="shield" /> Review with confidence
              </span>
            </div>

            <div className="lp-hero-dots" role="tablist" aria-label="Hero slideshow indicators">
              {heroImages.map((_, index) => (
                <button
                  key={index}
                  type="button"
                  role="tab"
                  className={`lp-hero-dot ${index === activeSlide ? 'active' : ''}`}
                  aria-label={`Show hero slide ${index + 1}`}
                  aria-selected={index === activeSlide}
                  onClick={() => setActiveSlide(index)}
                />
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* ====================================================================
          SECTION 2: WHAT SURPLUSLINK DOES (EDITORIAL TWO-COLUMN)
          ==================================================================== */}
      <section id="platform" className="lp-section-editorial">
        <div className="lp-container">
          <div className="lp-editorial-grid">
            <Reveal className="lp-editorial-left">
              <p className="lp-eyebrow">WHAT SURPLUSLINK DOES</p>
              <h2>More than a listing board.</h2>
              <p className="lp-editorial-lead">
                A modern material exchange built for the realities of construction logistics,
                tight project timelines, and accountable management oversight.
              </p>

              <div className="lp-journey" aria-label="Material lifecycle journey">
                <span className="lp-journey-eyebrow">MATERIAL JOURNEY</span>
                <div className="lp-journey-timeline">
                  <div className="lp-journey-line" aria-hidden="true" />

                  <div className="lp-journey-step">
                    <span className="lp-journey-node">
                      <span className="lp-journey-dot" />
                    </span>
                    <div className="lp-journey-text">
                      <strong className="lp-journey-label">Surplus Stock</strong>
                      <span className="lp-journey-desc">Verified inventory cataloged</span>
                    </div>
                  </div>

                  <div className="lp-journey-step">
                    <span className="lp-journey-node">
                      <span className="lp-journey-dot" />
                    </span>
                    <div className="lp-journey-text">
                      <strong className="lp-journey-label">Matched</strong>
                      <span className="lp-journey-desc">Specification & volume aligned</span>
                    </div>
                  </div>

                  <div className="lp-journey-step">
                    <span className="lp-journey-node">
                      <span className="lp-journey-dot" />
                    </span>
                    <div className="lp-journey-text">
                      <strong className="lp-journey-label">Logistics Checked</strong>
                      <span className="lp-journey-desc">Haulage route & viable transit</span>
                    </div>
                  </div>

                  <div className="lp-journey-step">
                    <span className="lp-journey-node">
                      <span className="lp-journey-dot" />
                    </span>
                    <div className="lp-journey-text">
                      <strong className="lp-journey-label">Human Reviewed</strong>
                      <span className="lp-journey-desc">Authorized supervisor sign-off</span>
                    </div>
                  </div>

                  <div className="lp-journey-step lp-journey-step-final">
                    <span className="lp-journey-node lp-journey-node-final">
                      <span className="lp-journey-dot lp-journey-dot-final" />
                    </span>
                    <div className="lp-journey-text">
                      <strong className="lp-journey-label lp-journey-label-final">Reused</strong>
                      <span className="lp-journey-desc">Put safely back to work on site</span>
                    </div>
                  </div>
                </div>
              </div>
            </Reveal>

            <Reveal className="lp-editorial-rows">
              <div className="lp-editorial-row">
                <span className="lp-row-num">01</span>
                <div className="lp-row-content">
                  <h3>Sellers listing reusable materials</h3>
                  <p>
                    Make available surplus inventory visible with precise condition, quantity,
                    specification, and site location before it becomes jobsite waste.
                  </p>
                </div>
              </div>

              <div className="lp-editorial-row">
                <span className="lp-row-num">02</span>
                <div className="lp-row-content">
                  <h3>Buyers publishing requirements</h3>
                  <p>
                    Specify exact material requirements, required quantities, target delivery
                    schedules, budget parameters, and designated project site destinations.
                  </p>
                </div>
              </div>

              <div className="lp-editorial-row">
                <span className="lp-row-num">03</span>
                <div className="lp-row-content">
                  <h3>Intelligent matching</h3>
                  <p>
                    Automatic compatibility evaluation based on material specifications, grade,
                    available volume, and transit feasibility between project sites.
                  </p>
                </div>
              </div>

              <div className="lp-editorial-row">
                <span className="lp-row-num">04</span>
                <div className="lp-row-content">
                  <h3>Logistics context</h3>
                  <p>
                    Haulage distance, transit duration, and vehicle practicality evaluated
                    alongside material cost savings, ensuring options are genuinely viable.
                  </p>
                </div>
              </div>

              <div className="lp-editorial-row">
                <span className="lp-row-num">05</span>
                <div className="lp-row-content">
                  <h3>Controlled human approval</h3>
                  <p>
                    Every workflow step is reviewed and authorized by designated project
                    supervisors to safeguard safety, contractual compliance, and quality.
                  </p>
                </div>
              </div>
            </Reveal>
          </div>
        </div>
      </section>

      {/* ====================================================================
          SECTION 3: HOW IT WORKS (CONNECTED TIMELINE)
          ==================================================================== */}
      <section id="how-it-works" className="lp-section-timeline">
        <div className="lp-container">
          <Reveal className="lp-section-header">
            <p className="lp-eyebrow">STEP-BY-STEP WORKFLOW</p>
            <h2>From surplus stock to project delivery.</h2>
            <p>
              A connected, accountable lifecycle designed to keep reusable construction materials
              moving toward their next purpose.
            </p>
          </Reveal>

          <div className="lp-timeline-track">
            <Reveal className="lp-timeline-step">
              <div className="lp-timeline-badge">
                <span className="lp-step-num">01</span>
              </div>
              <h3>01 LIST</h3>
              <p>Seller publishes surplus stock with verified specifications and location.</p>
            </Reveal>

            <Reveal className="lp-timeline-step">
              <div className="lp-timeline-badge">
                <span className="lp-step-num">02</span>
              </div>
              <h3>02 REQUEST</h3>
              <p>Buyer specifies material, quantity, location, budget and deadline.</p>
            </Reveal>

            <Reveal className="lp-timeline-step">
              <div className="lp-timeline-badge">
                <span className="lp-step-num">03</span>
              </div>
              <h3>03 MATCH</h3>
              <p>The system evaluates suitable materials, volume coverage, and logistics.</p>
            </Reveal>

            <Reveal className="lp-timeline-step">
              <div className="lp-timeline-badge">
                <span className="lp-step-num">04</span>
              </div>
              <h3>04 REVIEW</h3>
              <p>Buyer compares recommended options and selects suitable supply.</p>
            </Reveal>

            <Reveal className="lp-timeline-step">
              <div className="lp-timeline-badge">
                <span className="lp-step-num">05</span>
              </div>
              <h3>05 APPROVE</h3>
              <p>Authorized manager reviews and validates the selected workflow.</p>
            </Reveal>

            <Reveal className="lp-timeline-step">
              <div className="lp-timeline-badge">
                <span className="lp-step-num">06</span>
              </div>
              <h3>06 REUSE</h3>
              <p>Approved material moves to its next project, put safely back to work.</p>
            </Reveal>
          </div>
        </div>
      </section>

      {/* ====================================================================
          SECTION 4: PLATFORM FEATURES (BENTO GRID)
          ==================================================================== */}
      <section id="features" className="lp-section-features">
        <div className="lp-container">
          <Reveal className="lp-section-header">
            <p className="lp-eyebrow">PLATFORM CAPABILITIES</p>
            <h2>Built specifically for materials, logistics, and accountability.</h2>
            <p>
              Every capability addresses the practical friction points of construction material reuse.
            </p>
          </Reveal>

          <div className="lp-bento-grid">
            <Reveal className="lp-bento-card lp-bento-span-7">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="layers" />
                </div>
                <h3>Material inventory</h3>
                <p>
                  Structured cataloging for surplus lots with condition grading, batch sizes, unit
                  measurements, and geographic storage coordinates.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-bento-card lp-bento-span-5">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="search" />
                </div>
                <h3>Buyer requirements</h3>
                <p>
                  Structured demand intake capturing delivery deadlines, quantity bounds, budget
                  targets, and regional proximity constraints.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-bento-card lp-bento-span-4">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="check" />
                </div>
                <h3>Matching & recommendations</h3>
                <p>
                  Compatibility scoring combining material specifications, quantity alignment, and
                  haulage feasibility into clear options.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-bento-card lp-bento-span-4">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="route" />
                </div>
                <h3>Logistics context</h3>
                <p>
                  Built-in routing analysis estimating transit distance, travel times, and delivery
                  feasibility before formal commitments.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-bento-card lp-bento-span-4">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="shield" />
                </div>
                <h3>Human approval</h3>
                <p>
                  Designated manager review governing reservations, compliance verifications, and
                  formal workflow sign-offs.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-bento-card lp-bento-span-6">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="box" />
                </div>
                <h3>Stock reservation</h3>
                <p>
                  Real-time inventory locks protect matched material lots against duplicate allocations
                  while manager approvals are pending.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-bento-card lp-bento-span-6">
              <div>
                <div className="lp-bento-icon">
                  <Icon name="file-text" />
                </div>
                <h3>Transaction tracking</h3>
                <p>
                  End-to-end transparent record keeping documenting material origins, match evaluations,
                  and complete handover history.
                </p>
              </div>
            </Reveal>
          </div>
        </div>
      </section>

      {/* ====================================================================
          SECTION 5: MATERIAL TYPES (PHOTO-BACKED CARDS)
          ==================================================================== */}
      <section id="materials" className="lp-section-materials">
        <div className="lp-container">
          <Reveal className="lp-section-header">
            <p className="lp-eyebrow">REUSABLE MATERIAL CATALOG</p>
            <h2>High-value categories ready for circulation.</h2>
            <p>
              Designed around primary construction trades with specialized specifications and handling.
            </p>
          </Reveal>

          <div className="lp-materials-grid">
            <Reveal className="lp-material-card">
              <img 
                className="lp-material-bg" 
                src="/images/hero/tiles-and-timber.png" 
                alt="Tiles and finishes" 
              />
              <div className="lp-material-overlay" />
              <div className="lp-material-content">
                <span className="lp-material-tag">SURPLUS FINISHES</span>
                <h3>Tiles & finishes</h3>
                <p>
                  Ceramic, porcelain, natural stone tiles, pavers, architectural wall finishes, and
                  surplus commercial fit-out packages.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-material-card">
              <img 
                className="lp-material-bg" 
                src="/images/hero/steel-loading-bay.png" 
                alt="Steel and structural materials" 
              />
              <div className="lp-material-overlay" />
              <div className="lp-material-content">
                <span className="lp-material-tag">STRUCTURAL STOCK</span>
                <h3>Steel & structural</h3>
                <p>
                  Reinforcement bar, structural sections, universal beams, hollow pipes, steel mesh,
                  and usable metal components.
                </p>
              </div>
            </Reveal>

            <Reveal className="lp-material-card">
              <img 
                className="lp-material-bg" 
                src="/images/hero/warehouse-timber.png" 
                alt="Timber and components" 
              />
              <div className="lp-material-overlay" />
              <div className="lp-material-content">
                <span className="lp-material-tag">RECLAIMED LUMBER</span>
                <h3>Timber & components</h3>
                <p>
                  Reclaimed framing studs, structural beams, plywood sheets, engineered trusses, pallets,
                  and architectural joinery.
                </p>
              </div>
            </Reveal>
          </div>
        </div>
      </section>

      {/* ====================================================================
          SECTION 6: SUSTAINABILITY & IMPACT
          ==================================================================== */}
      <section id="impact" className="lp-section-impact">
        <div className="lp-container">
          <Reveal className="lp-section-header">
            <p className="lp-eyebrow">SUSTAINABLE CIRCULARITY</p>
            <h2>Use what already exists.</h2>
            <p>
              Construction materials often retain full structural integrity after project handovers.
              Making that value visible ensures they reach another project rather than disposal.
            </p>
          </Reveal>

          <div className="lp-impact-grid">
            <Reveal className="lp-impact-item">
              <div className="lp-impact-icon">
                <Icon name="recycle" />
              </div>
              <h3>Keep materials in use</h3>
              <p>
                Extend the functional lifecycle of quality construction supplies and preserve the embodied
                energy invested in manufacturing.
              </p>
            </Reveal>

            <Reveal className="lp-impact-item">
              <div className="lp-impact-icon">
                <Icon name="box" />
              </div>
              <h3>Reduce unnecessary disposal</h3>
              <p>
                Divert pristine over-orders, clean offcuts, and salvageable building components away from
                costly commercial landfills.
              </p>
            </Reveal>

            <Reveal className="lp-impact-item">
              <div className="lp-impact-icon">
                <Icon name="search" />
              </div>
              <h3>Improve stock visibility</h3>
              <p>
                Transform dispersed jobsite surplus into searchable, categorized digital inventory accessible
                across local construction networks.
              </p>
            </Reveal>

            <Reveal className="lp-impact-item">
              <div className="lp-impact-icon">
                <Icon name="route" />
              </div>
              <h3>Source closer to projects</h3>
              <p>
                Prioritize nearby regional supply to curtail transport fuel emissions, reduce haulage
                lead times, and cut shipping overheads.
              </p>
            </Reveal>
          </div>

          <Reveal className="lp-impact-banner">
            <div className="lp-banner-text">
              <strong>Accountable, practical sustainability</strong>
              <span>
                Every material match is grounded in transparent specifications, verified logistics, and
                management approval.
              </span>
            </div>
            <span className="lp-banner-tag">
              <Icon name="shield" /> Verified Governance
            </span>
          </Reveal>
        </div>
      </section>

      {/* ====================================================================
          SECTION 7: FINAL CTA (WARM LIGHT GRADIENT - NO NAVY)
          ==================================================================== */}
      <section id="cta" className="lp-section-cta">
        <div className="lp-container">
          <Reveal className="lp-cta-card">
            <p className="lp-eyebrow">GET STARTED TODAY</p>
            <h2>Start with what you already have.</h2>
            <p>
              Whether cataloging surplus materials from completed building phases or sourcing quality
              stock for your next project, SurplusLink connects you directly to practical opportunities.
            </p>
            <div className="lp-cta-actions">
              <Link className="lp-btn-primary" to={accountPath}>
                {authenticated ? 'Open Dashboard' : 'Login'} <Icon name="arrow" />
              </Link>
              <a 
                className="lp-btn-cta-secondary" 
                href="#how-it-works" 
                onClick={(e) => handleSmoothScroll(e, 'how-it-works')}
              >
                How it works
              </a>
            </div>
          </Reveal>
        </div>
      </section>

      {/* ====================================================================
          8. FOOTER
          ==================================================================== */}
      <footer className="lp-footer">
        <div className="lp-container">
          <div className="lp-footer-grid">
            <div className="lp-footer-brand">
              <a 
                className="lp-brand" 
                href="#top" 
                onClick={(e) => handleSmoothScroll(e, 'top')}
              >
                <img 
                  className="lp-brand-img" 
                  src="/images/brand/surpluslink-mark.png" 
                  alt="SurplusLink" 
                />
                <span>SurplusLink</span>
              </a>
              <p>
                The accountable marketplace for reusable construction materials. Connecting supply,
                demand, and logistics.
              </p>
            </div>

            <div className="lp-footer-col">
              <h4>Platform</h4>
              <a href="#platform" onClick={(e) => handleSmoothScroll(e, 'platform')}>
                What it does
              </a>
              <a href="#how-it-works" onClick={(e) => handleSmoothScroll(e, 'how-it-works')}>
                How it works
              </a>
              <a href="#features" onClick={(e) => handleSmoothScroll(e, 'features')}>
                Capabilities
              </a>
            </div>

            <div className="lp-footer-col">
              <h4>Exchange</h4>
              <a href="#materials" onClick={(e) => handleSmoothScroll(e, 'materials')}>
                Material types
              </a>
              <a href="#impact" onClick={(e) => handleSmoothScroll(e, 'impact')}>
                Sustainability
              </a>
              <a href="#cta" onClick={(e) => handleSmoothScroll(e, 'cta')}>
                Get started
              </a>
            </div>

            <div className="lp-footer-col">
              <h4>Access</h4>
              <Link to={accountPath}>
                {authenticated ? 'Open dashboard' : 'Account login'}
              </Link>
              <span>Construction circularity</span>
            </div>
          </div>

          <div className="lp-footer-bottom">
            <span>&copy; {new Date().getFullYear()} SurplusLink. All rights reserved.</span>
            <span>Sustainable construction material reuse platform</span>
          </div>
        </div>
      </footer>
    </div>
  );
}

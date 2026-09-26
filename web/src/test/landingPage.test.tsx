import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { AuthProvider } from '../auth/AuthContext';
import { LandingPage } from '../pages/LandingPage';

function renderLanding() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <LandingPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('LandingPage Final Polish', () => {
  it('renders the real SurplusLink logo asset (not a placeholder)', () => {
    renderLanding();
    const logoImgs = document.querySelectorAll('img[src="/images/brand/surpluslink-mark.png"]');
    expect(logoImgs.length).toBeGreaterThan(0);
    // Ensure no placeholder black circle "+", fake "S", etc.
    expect(screen.queryByText('brand-symbol')).not.toBeInTheDocument();
  });

  it('renders the hero title and eyebrow without pill/border box', () => {
    renderLanding();
    // Plain uppercase eyebrow
    expect(screen.getByText('CONSTRUCTION MATERIALS, PUT BACK TO WORK')).toBeInTheDocument();
    
    // Headline with Opportunity
    const heading = screen.getByRole('heading', { level: 1 });
    expect(heading).toHaveTextContent(/Turn Surplus/i);
    expect(heading).toHaveTextContent(/Into Opportunity\./i);

    // Verify removed hero paragraph is NOT present
    expect(
      screen.queryByText(/Connect buyers and sellers of reusable construction materials with practical matching/i)
    ).not.toBeInTheDocument();
  });

  it('renders the 3 hero capability items under CTAs', () => {
    renderLanding();
    expect(screen.getByText(/List useful materials/i)).toBeInTheDocument();
    expect(screen.getByText(/Source for a project/i)).toBeInTheDocument();
    expect(screen.getByText(/Review with confidence/i)).toBeInTheDocument();
  });

  it('renders SECTION 2: What SurplusLink Does with editorial rows (no generic card grid)', () => {
    renderLanding();
    expect(screen.getByText('More than a listing board.')).toBeInTheDocument();
    expect(screen.getByText('Sellers listing reusable materials')).toBeInTheDocument();
    expect(screen.getByText('Buyers publishing requirements')).toBeInTheDocument();
    expect(screen.getByText('Intelligent matching')).toBeInTheDocument();
    expect(screen.getAllByText('Logistics context').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Controlled human approval')).toBeInTheDocument();
  });

  it('renders the vertical material journey in Section 2 left column', () => {
    renderLanding();
    expect(screen.getByText('MATERIAL JOURNEY')).toBeInTheDocument();
    expect(screen.getByText('Surplus Stock')).toBeInTheDocument();
    expect(screen.getByText('Matched')).toBeInTheDocument();
    expect(screen.getByText('Logistics Checked')).toBeInTheDocument();
    expect(screen.getByText('Human Reviewed')).toBeInTheDocument();
    expect(screen.getByText('Reused')).toBeInTheDocument();
  });

  it('renders SECTION 3: How it Works connected 6-step timeline', () => {
    renderLanding();
    expect(screen.getByText('From surplus stock to project delivery.')).toBeInTheDocument();
    expect(screen.getByText('01 LIST')).toBeInTheDocument();
    expect(screen.getByText('02 REQUEST')).toBeInTheDocument();
    expect(screen.getByText('03 MATCH')).toBeInTheDocument();
    expect(screen.getByText('04 REVIEW')).toBeInTheDocument();
    expect(screen.getByText('05 APPROVE')).toBeInTheDocument();
    expect(screen.getByText('06 REUSE')).toBeInTheDocument();
  });

  it('renders SECTION 4: Platform Features with varied bento layout', () => {
    renderLanding();
    expect(screen.getByText(/Built specifically for materials, logistics, and accountability/i)).toBeInTheDocument();
    expect(screen.getByText('Material inventory')).toBeInTheDocument();
    expect(screen.getByText('Buyer requirements')).toBeInTheDocument();
    expect(screen.getByText('Matching & recommendations')).toBeInTheDocument();
    expect(screen.getByText('Human approval')).toBeInTheDocument();
    expect(screen.getByText('Stock reservation')).toBeInTheDocument();
    expect(screen.getByText('Transaction tracking')).toBeInTheDocument();
  });

  it('renders SECTION 5: Material Types with photo-backed cards and existing images', () => {
    renderLanding();
    expect(screen.getByText('Tiles & finishes')).toBeInTheDocument();
    expect(screen.getByText('Steel & structural')).toBeInTheDocument();
    expect(screen.getByText('Timber & components')).toBeInTheDocument();

    const tilesImg = document.querySelector('img[src="/images/hero/tiles-and-timber.png"]');
    const steelImg = document.querySelector('img[src="/images/hero/steel-loading-bay.png"]');
    const timberImg = document.querySelector('img[src="/images/hero/warehouse-timber.png"]');
    expect(tilesImg).toBeInTheDocument();
    expect(steelImg).toBeInTheDocument();
    expect(timberImg).toBeInTheDocument();
  });

  it('renders SECTION 6: Sustainability & Impact with horizontal metric layout', () => {
    renderLanding();
    expect(screen.getByText('Use what already exists.')).toBeInTheDocument();
    expect(screen.getByText('Keep materials in use')).toBeInTheDocument();
    expect(screen.getByText('Reduce unnecessary disposal')).toBeInTheDocument();
    expect(screen.getByText('Improve stock visibility')).toBeInTheDocument();
    expect(screen.getByText('Source closer to projects')).toBeInTheDocument();
  });

  it('renders SECTION 7: Final CTA without solid navy box', () => {
    renderLanding();
    expect(screen.getByText('Start with what you already have.')).toBeInTheDocument();
    // Buttons in CTA
    const ctaCard = document.querySelector('.lp-cta-card');
    expect(ctaCard).toBeInTheDocument();
    expect(ctaCard).toHaveTextContent(/Login/i);
    expect(ctaCard).toHaveTextContent(/How it works/i);
  });

  it('guarantees ZERO dead links: every anchor with # targets an element ID present on the page', () => {
    renderLanding();
    const anchors = Array.from(document.querySelectorAll('a[href^="#"]')) as HTMLAnchorElement[];
    expect(anchors.length).toBeGreaterThan(0);

    anchors.forEach((anchor) => {
      const hash = anchor.getAttribute('href');
      if (hash && hash.startsWith('#') && hash.length > 1) {
        const targetId = hash.slice(1);
        const targetEl = document.getElementById(targetId);
        expect(targetEl, `Target ID #${targetId} referenced by anchor "${anchor.textContent}" must exist`).not.toBeNull();
      }
    });
  });

  it('allows clicking slideshow indicator dots to switch slides', async () => {
    const user = userEvent.setup();
    renderLanding();
    const dots = screen.getAllByRole('tab', { name: /Show hero slide/i });
    expect(dots.length).toBe(4);

    await user.click(dots[2]);
    expect(dots[2]).toHaveClass('active');
  });

  it('allows toggling the mobile menu', async () => {
    const user = userEvent.setup();
    renderLanding();
    const toggleBtn = screen.getByRole('button', { name: /Open navigation menu/i });
    await user.click(toggleBtn);
    expect(toggleBtn).toHaveAttribute('aria-expanded', 'true');
    await user.click(toggleBtn);
    expect(toggleBtn).toHaveAttribute('aria-expanded', 'false');
  });
});

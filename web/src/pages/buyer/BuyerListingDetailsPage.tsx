import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  fetchMarketplaceListingDetails,
  type MaterialListingItem,
} from '../../api/buyerMarketplaceApi';
import { MobileHandoffModal } from './MobileHandoffModal';
import { resolveImageUrl, handleImageError } from '../../utils/imageUrl';
import './buyerMarketplace.css';

export function BuyerListingDetailsPage() {
  const { listingId } = useParams<{ listingId: string }>();
  const [listing, setListing] = useState<MaterialListingItem | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedPhotoIndex, setSelectedPhotoIndex] = useState(0);
  const [isHandoffModalOpen, setIsHandoffModalOpen] = useState(false);

  useEffect(() => {
    if (!listingId) return;

    let isMounted = true;
    setLoading(true);
    setError(null);

    fetchMarketplaceListingDetails(listingId)
      .then((data) => {
        if (isMounted) {
          setListing(data);
          setSelectedPhotoIndex(0);
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setError(err instanceof Error ? err.message : 'Failed to load listing details.');
        }
      })
      .finally(() => {
        if (isMounted) {
          setLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [listingId]);

  if (loading) {
    return (
      <div className="details-page-shell" style={{ textAlign: 'center', padding: '5rem 0' }}>
        <div className="spinner" style={{ margin: '0 auto 1.5rem' }} />
        <p style={{ color: 'var(--sl-market-text-muted)', fontWeight: 600 }}>
          Loading material details...
        </p>
      </div>
    );
  }

  if (error || !listing) {
    return (
      <div className="details-page-shell">
        <nav className="details-breadcrumb" aria-label="Breadcrumb">
          <Link to="/app/buyer">&larr; Back to Marketplace</Link>
        </nav>
        <div className="marketplace-empty-state">
          <h2 className="empty-state-title">Listing Not Found</h2>
          <p className="empty-state-text">
            {error || 'The requested material listing could not be found or has expired.'}
          </p>
          <Link to="/app/buyer" className="button button-primary">
            Return to Marketplace
          </Link>
        </div>
      </div>
    );
  }

  const sortedPhotos = [...(listing.photos || [])].sort((a, b) => a.sortOrder - b.sortOrder);
  const rawPhoto = sortedPhotos[selectedPhotoIndex]?.photoUrl || sortedPhotos[0]?.photoUrl;
  const currentPhoto = rawPhoto ? resolveImageUrl(rawPhoto) : null;
  const availableQuantity = Math.max(0, listing.quantity - (listing.reservedQuantity || 0));
  const sellerDisplayName = listing.seller?.businessName || 'Verified Construction Supplier';

  return (
    <div className="details-page-shell">
      <nav className="details-breadcrumb" aria-label="Breadcrumb">
        <Link to="/app/buyer">&larr; Back to Marketplace</Link>
        <span aria-hidden="true">/</span>
        <span>{listing.categoryName}</span>
        <span aria-hidden="true">/</span>
        <span style={{ color: 'var(--sl-market-text-main)' }}>{listing.title}</span>
      </nav>

      <div className="details-layout">
        {/* Left Column: Image Gallery */}
        <div className="details-gallery">
          <div className="details-primary-image-container">
            {currentPhoto ? (
              <img
                src={currentPhoto}
                alt={listing.title}
                className="details-primary-image"
                onError={handleImageError}
              />
            ) : (
              <div className="marketplace-card-placeholder" style={{ height: '100%' }}>
                <svg
                  width="48"
                  height="48"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.5"
                >
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                  <circle cx="8.5" cy="8.5" r="1.5" />
                  <polyline points="21 15 16 10 5 21" />
                </svg>
                <span>No images provided</span>
              </div>
            )}
          </div>

          {sortedPhotos.length > 1 && (
            <div className="details-thumbnails" role="tablist" aria-label="Material images">
              {sortedPhotos.map((photo, idx) => (
                <button
                  key={photo.id || idx}
                  type="button"
                  className={`details-thumb-btn ${idx === selectedPhotoIndex ? 'active' : ''}`}
                  onClick={() => setSelectedPhotoIndex(idx)}
                  aria-label={`View photo ${idx + 1}`}
                >
                  <img
                    src={resolveImageUrl(photo.photoUrl)}
                    alt=""
                    className="details-thumb-img"
                    onError={handleImageError}
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Right Column: Listing Details & CTA */}
        <div className="details-content">
          <div className="details-meta-top">
            <span className="marketplace-card-category">{listing.categoryName}</span>
            <span className={`condition-pill condition-${listing.condition.toLowerCase()}`}>
              {listing.condition}
            </span>
          </div>

          <h1 className="details-title">{listing.title}</h1>

          <div className="details-price-panel">
            <div className="details-price-label">Unit Price</div>
            <div>
              <span className="details-price-amount">
                LKR {listing.unitPrice.toLocaleString()}
              </span>
              <span className="details-price-unit"> / {listing.unit}</span>
            </div>
          </div>

          <div className="details-specs-grid">
            <div className="spec-item">
              <div className="spec-label">Available Stock</div>
              <div className="spec-value" style={{ color: '#15803d' }}>
                {availableQuantity.toLocaleString()} {listing.unit}
              </div>
            </div>

            <div className="spec-item">
              <div className="spec-label">Total Listed</div>
              <div className="spec-value">
                {listing.quantity.toLocaleString()} {listing.unit}
              </div>
            </div>

            <div className="spec-item">
              <div className="spec-label">Supplier</div>
              <div className="spec-value">{sellerDisplayName}</div>
            </div>

            <div className="spec-item">
              <div className="spec-label">Listing Status</div>
              <div className="spec-value" style={{ textTransform: 'capitalize' }}>
                {listing.status.toLowerCase()}
              </div>
            </div>
          </div>

          <div className="details-description-box">
            <div className="details-section-label">Description</div>
            <p className="details-description-text">{listing.description}</p>
          </div>

          {/* AI Mobile CTA Section - NO direct buy flow */}
          <div className="ai-mobile-cta-section">
            <div className="ai-mobile-badge">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2" />
              </svg>
              <span>AI Matching Workflow</span>
            </div>

            <h2 className="ai-mobile-heading">Looking for the best match for your project?</h2>

            <p className="ai-mobile-desc">
              SurplusLink Mobile can compare available materials in this category using your required quantity, budget, location and logistics.
            </p>

            <button
              type="button"
              className="ai-mobile-launch-btn"
              onClick={() => setIsHandoffModalOpen(true)}
            >
              Continue with SurplusLink Mobile
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <path d="M5 12h14" />
                <path d="m12 5 7 7-7 7" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      {/* QR Handoff Modal */}
      <MobileHandoffModal
        isOpen={isHandoffModalOpen}
        onClose={() => setIsHandoffModalOpen(false)}
        categoryId={listing.categoryId}
        categoryName={listing.categoryName}
      />
    </div>
  );
}

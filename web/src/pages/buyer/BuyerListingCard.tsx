import React from 'react';
import { Link } from 'react-router-dom';
import type { MaterialListingItem } from '../../api/buyerMarketplaceApi';
import { resolveImageUrl, handleImageError } from '../../utils/imageUrl';

export interface BuyerListingCardProps {
  listing: MaterialListingItem;
}

export function BuyerListingCard({ listing }: BuyerListingCardProps) {
  const sortedPhotos = [...(listing.photos || [])].sort((a, b) => a.sortOrder - b.sortOrder);
  const rawPrimaryPhoto = sortedPhotos[0]?.photoUrl;
  const primaryPhoto = rawPrimaryPhoto ? resolveImageUrl(rawPrimaryPhoto) : null;

  const availableQuantity = Math.max(0, listing.quantity - (listing.reservedQuantity || 0));
  const sellerDisplayName = listing.seller?.businessName || 'Verified Supplier';

  const conditionClass = `condition-${listing.condition.toLowerCase()}`;

  return (
    <Link
      to={`/app/buyer/materials/${listing.id}`}
      className="marketplace-card"
      data-testid={`listing-card-${listing.id}`}
    >
      <div className="marketplace-card-accent-line" />

      <div className="marketplace-card-media">
        {primaryPhoto ? (
          <img
            src={primaryPhoto}
            alt={listing.title}
            className="marketplace-card-img"
            loading="lazy"
            onError={handleImageError}
          />
        ) : (
          <div className="marketplace-card-placeholder">
            <svg
              width="36"
              height="36"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <circle cx="8.5" cy="8.5" r="1.5" />
              <polyline points="21 15 16 10 5 21" />
            </svg>
            <span style={{ fontSize: '0.78rem' }}>No image available</span>
          </div>
        )}

        <div className="marketplace-card-badge-top">
          <span className={`condition-pill ${conditionClass}`}>{listing.condition}</span>
        </div>
      </div>

      <div className="marketplace-card-body">
        <span className="marketplace-card-category">{listing.categoryName}</span>
        <h3 className="marketplace-card-title">{listing.title}</h3>

        <div className="marketplace-card-price-row">
          <span className="marketplace-card-price-currency">LKR</span>
          <span className="marketplace-card-price-value">
            {listing.unitPrice.toLocaleString()}
          </span>
          <span className="marketplace-card-price-unit">/ {listing.unit}</span>
        </div>

        <div className="marketplace-card-stock">
          {availableQuantity.toLocaleString()} {listing.unit} available
        </div>

        <div className="marketplace-card-footer">
          <span className="marketplace-card-seller" title={sellerDisplayName}>
            {sellerDisplayName}
          </span>
          <svg
            className="marketplace-card-action-icon"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <path d="M5 12h14" />
            <path d="m12 5 7 7-7 7" />
          </svg>
        </div>
      </div>
    </Link>
  );
}

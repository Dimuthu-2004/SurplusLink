import { formatLkr } from '../../utils/currency';
import { formatMaterialQuantity } from '../../features/materials/quantityFormat';
import { handleImageError, resolveImageUrl } from '../../utils/imageUrl';
import { useEffect, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  managerMaterialsApi,
  type ListingHistoryItem,
  type ManagerMaterialsApi,
  type MaterialListing,
} from '../../features/materials/managerMaterialsApi';
import { SuccessOverlay } from '../../components/StatusAnimation';

export function ManagerMaterialDetailsPage({
  api = managerMaterialsApi,
}: {
  api?: ManagerMaterialsApi;
}) {
  const { listingId } = useParams();
  const location = useLocation();
  const fromApprovals = Boolean(location.state?.fromListingApprovals);
  const [listing, setListing] = useState<MaterialListing | null>(null);
  const [history, setHistory] = useState<ListingHistoryItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [photoIndex, setPhotoIndex] = useState<number | null>(null);
  const [showApprovalSuccess, setShowApprovalSuccess] = useState(false);

  const load = async () => {
    if (!listingId) {
      setError('The listing identifier is missing.');
      return;
    }
    setError(null);
    setListing(null);
    try {
      const [loadedListing, loadedHistory] = await Promise.all([
        api.getListing(listingId),
        api.getHistory(listingId),
      ]);
      setListing(loadedListing);
      setHistory(loadedHistory);
    } catch (reason) {
      setError(messageFor(reason));
    }
  };

  useEffect(() => {
    void load();
  }, [api, listingId]);

  async function verify(approved: boolean) {
    if (!listing || saving) return;
    setSaving(true);
    setError(null);
    try {
      setListing(await api.verifyListing(listing.id, approved));
      setHistory(await api.getHistory(listing.id));
      if (approved) setShowApprovalSuccess(true);
    } catch (reason) {
      setError(messageFor(reason));
    } finally {
      setSaving(false);
    }
  }

  if (error && !listing) {
    return (
      <section className="manager-panel">
        <p className="error-message" role="alert">{error}</p>
        <button className="button button-secondary" type="button" onClick={() => void load()}>Retry</button>
      </section>
    );
  }

  if (!listing) {
    return <section className="manager-panel" aria-live="polite">Loading listing...</section>;
  }

  const canVerify = listing.status === 'PENDING_VERIFICATION';

  return (
    <div className="manager-page">
      {showApprovalSuccess && <SuccessOverlay kind="approval" title="Approved successfully" message="Seller listing approved." onComplete={() => setShowApprovalSuccess(false)} />}
      <Link className="back-link" to={fromApprovals ? "/app/manager/listing-approvals" : "/app/manager/materials"}>{fromApprovals ? "Back to Seller Listing Approvals" : "Back to all listings"}</Link>
      {error && <p className="error-message" role="alert">{error}</p>}
      <section className="manager-panel">
        <div className="section-heading">
          <div>
            <p className="eyebrow">{listing.categoryName}</p>
            <h1>{listing.title}</h1>
          </div>
          <span className="status-badge">{listing.status.replaceAll('_', ' ')}</span>
        </div>
        <p>{listing.description}</p>
        <dl className="detail-grid">
          <Detail label="Seller" value={listing.seller?.fullName || 'Profile not completed'} />
          <Detail label="Business" value={listing.seller?.businessName || 'Not provided'} />
          <Detail label="Seller email" value={listing.seller?.email || 'Not provided'} />
          <Detail label="Seller phone" value={listing.seller?.phoneNumber || 'Not provided'} />
          <Detail label="Condition" value={listing.condition} />
          <Detail label="Item" value={listing.constructionItemTemplateName || (listing.isCustomPendingReview ? 'Custom item (manager review)' : listing.title)} />
          <Detail label="Total stock" value={stockSummary(listing, 'total')} />
          <Detail label="Reserved" value={stockSummary(listing, 'reserved')} />
          <Detail label="Available stock" value={stockSummary(listing, 'available')} />
          {listing.quantityMode === 'PACKAGE' || listing.quantityMode === 'PIECE' ? <>
            <Detail label="Package size" value={listing.packageSize ? `${formatMaterialQuantity(listing.packageSize, listing.baseUnit || listing.unit)} ${listing.baseUnit || listing.unit} / ${listing.packageType?.toLowerCase() || 'package'}` : 'Not recorded'} />
            <Detail label="Base-equivalent quantity" value={`${formatMaterialQuantity(listing.quantity, listing.baseUnit || listing.unit)} ${listing.baseUnit || listing.unit}`} />
          </> : null}
          <Detail label="Price" value={`${formatPrice(listing.unitPrice)} / ${listing.quantityMode === 'PACKAGE' || listing.quantityMode === 'PIECE' ? (listing.packageType?.toLowerCase() || 'item') : listing.unit}`} />
          <Detail label="Available until" value={formatDate(listing.availableUntil)} />
          <Detail label="Created" value={formatDate(listing.createdAtUtc)} />
          <Detail label="Updated" value={formatDate(listing.updatedAtUtc)} />
        </dl>
        {listing.latitude != null && listing.longitude != null && <a className="back-link" href={`https://www.google.com/maps/search/?api=1&query=${listing.latitude},${listing.longitude}`} target="_blank" rel="noreferrer">View material location in Google Maps</a>}
        {specificationRows(listing.specificationsJson).length > 0 && <dl className="detail-grid">{specificationRows(listing.specificationsJson).map(([label, value]) => <Detail key={label} label={label} value={value} />)}</dl>}
        {listing.photos.length > 0 && (
          <div className="photo-grid" aria-label="Listing photos">
            {listing.photos.map((photo, index) => <button className="listing-photo-button" type="button" key={photo.id} onClick={() => setPhotoIndex(index)}><img src={resolveImageUrl(photo.photoUrl)} onError={handleImageError} alt={`View ${listing.title} photo ${index + 1}`} /></button>)}
          </div>
        )}
        {canVerify && (
          <div className="action-row">
            <button className="button button-primary" type="button" disabled={saving} onClick={() => void verify(true)}>
              {saving ? 'Saving...' : 'Verify listing'}
            </button>
            <button className="button button-danger" type="button" disabled={saving} onClick={() => void verify(false)}>
              Reject listing
            </button>
          </div>
        )}
      </section>

      {photoIndex !== null && <ListingPhotoViewer listing={listing} index={photoIndex} onClose={() => setPhotoIndex(null)} onChange={setPhotoIndex} />}

      <section className="manager-panel" aria-labelledby="history-heading">
        <h2 id="history-heading">Listing history</h2>
        {history.length === 0 ? <p className="empty-state">No history has been recorded yet.</p> : (
          <ol className="history-list">
            {history.map((item) => (
              <li key={item.id}>
                <strong>{historyLabel(item.action)}</strong>
                <span>{formatDate(item.createdAtUtc)} - {item.actorUserId ?? 'System'}</span>
              </li>
            ))}
          </ol>
        )}
      </section>
    </div>
  );
}

function ListingPhotoViewer({ listing, index, onClose, onChange }: { listing: MaterialListing; index: number; onClose: () => void; onChange: (index: number) => void }) {
  const photo = listing.photos[index];
  return <div className="listing-lightbox" role="dialog" aria-modal="true" aria-label="Listing photo viewer" onMouseDown={onClose}>
    <div className="listing-lightbox-content" onMouseDown={event => event.stopPropagation()}>
      <button className="listing-lightbox-close" type="button" onClick={onClose} aria-label="Close photo viewer">Close</button>
      {listing.photos.length > 1 && <button className="listing-lightbox-nav previous" type="button" onClick={() => onChange((index - 1 + listing.photos.length) % listing.photos.length)} aria-label="Previous photo">Previous</button>}
      <img src={resolveImageUrl(photo.photoUrl)} onError={handleImageError} alt={`${listing.title} photo ${index + 1}`} />
      {listing.photos.length > 1 && <button className="listing-lightbox-nav next" type="button" onClick={() => onChange((index + 1) % listing.photos.length)} aria-label="Next photo">Next</button>}
      <p>{index + 1} of {listing.photos.length}</p>
    </div>
  </div>;
}

function stockSummary(listing: MaterialListing, kind: 'total' | 'reserved' | 'available'): string {
  if (listing.quantityMode === 'PACKAGE' || listing.quantityMode === 'PIECE') { const total = listing.packageCount ?? 0; const reserved = listing.reservedPackageCount ?? 0; return `${kind === 'total' ? total : kind === 'reserved' ? reserved : total - reserved} ${listing.packageType?.toLowerCase() || 'items'}`; }
  return `${formatMaterialQuantity(kind === 'total' ? listing.quantity : kind === 'reserved' ? listing.reservedQuantity : (listing.availableQuantity ?? listing.quantity - listing.reservedQuantity), listing.unit)} ${listing.unit}`;
}

function specificationRows(value?: string | null): [string, string][] {
  if (!value) return [];
  try {
    const parsed: unknown = JSON.parse(value);
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return [];
    return Object.entries(parsed).filter(([, item]) => item != null && String(item).trim() !== '').map(([key, item]) => [key.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, c => c.toUpperCase()), String(item)]);
  } catch { return []; }
}

function historyLabel(action: string): string {
  const labels: Record<string, string> = {
    LISTING_CREATED: 'Draft saved', LISTING_UPDATED: 'Listing edited', LISTING_EDITED_AFTER_REJECTION: 'Edited after rejection',
    LISTING_SUBMITTED_FOR_VERIFICATION: 'Submitted for verification', LISTING_RESUBMITTED_FOR_VERIFICATION: 'Resubmitted for verification',
    LISTING_VERIFIED: 'Approved', LISTING_REJECTED: 'Rejected', LISTING_DELETED: 'Listing closed',
  };
  return labels[action] ?? action.replaceAll('_', ' ');
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>;
}

function formatPrice(value: number): string {
  return formatLkr(value);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function messageFor(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Unable to load this listing.';
}

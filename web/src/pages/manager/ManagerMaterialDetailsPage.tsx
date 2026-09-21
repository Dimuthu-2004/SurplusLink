import { environment } from '../../config/environment';
import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  managerMaterialsApi,
  type ListingHistoryItem,
  type ManagerMaterialsApi,
  type MaterialListing,
} from '../../features/materials/managerMaterialsApi';

export function ManagerMaterialDetailsPage({
  api = managerMaterialsApi,
}: {
  api?: ManagerMaterialsApi;
}) {
  const { listingId } = useParams();
  const [listing, setListing] = useState<MaterialListing | null>(null);
  const [history, setHistory] = useState<ListingHistoryItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

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
      <Link className="back-link" to="/app/manager/materials">Back to all listings</Link>
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
          <Detail label="Quantity" value={`${listing.quantity} ${listing.unit}`} />
          <Detail label="Reserved" value={`${listing.reservedQuantity} ${listing.unit}`} />
          <Detail label="Unit price" value={formatPrice(listing.unitPrice)} />
          <Detail label="Available until" value={formatDate(listing.availableUntil)} />
          <Detail label="Created" value={formatDate(listing.createdAtUtc)} />
          <Detail label="Updated" value={formatDate(listing.updatedAtUtc)} />
        </dl>
        {listing.latitude != null && listing.longitude != null && <a className="back-link" href={`https://www.google.com/maps/search/?api=1&query=${listing.latitude},${listing.longitude}`} target="_blank" rel="noreferrer">View material location in Google Maps</a>}
        {listing.photos.length > 0 && (
          <div className="photo-grid" aria-label="Listing photos">
            {listing.photos.map((photo) => <img key={photo.id} src={new URL(photo.photoUrl, environment.apiBaseUrl).toString()} alt={listing.title} />)}
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

      <section className="manager-panel" aria-labelledby="history-heading">
        <h2 id="history-heading">Listing history</h2>
        {history.length === 0 ? <p className="empty-state">No history has been recorded yet.</p> : (
          <ol className="history-list">
            {history.map((item) => (
              <li key={item.id}>
                <strong>{item.action}</strong>
                <span>{formatDate(item.createdAtUtc)} - {item.actorUserId ?? 'System'}</span>
              </li>
            ))}
          </ol>
        )}
      </section>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>;
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function messageFor(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Unable to load this listing.';
}

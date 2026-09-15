import { useEffect, useState } from 'react';
import {
  managerMaterialsApi,
  type InventoryAnalytics,
  type ManagerMaterialsApi,
} from './managerMaterialsApi';

export function InventoryAnalyticsWidget({
  api = managerMaterialsApi,
}: {
  api?: ManagerMaterialsApi;
}) {
  const [analytics, setAnalytics] = useState<InventoryAnalytics | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      setAnalytics(await api.getAnalytics());
    } catch (reason) {
      setError(messageFor(reason));
    }
  };

  useEffect(() => {
    void load();
  }, [api]);

  if (error) {
    return (
      <section className="manager-panel" aria-labelledby="analytics-heading">
        <h2 id="analytics-heading">Inventory analytics</h2>
        <p className="error-message" role="alert">{error}</p>
        <button className="button button-secondary" type="button" onClick={() => void load()}>
          Retry analytics
        </button>
      </section>
    );
  }

  if (!analytics) {
    return <section className="manager-panel">Loading inventory analytics...</section>;
  }

  return (
    <section className="manager-panel" aria-labelledby="analytics-heading">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Live overview</p>
          <h2 id="analytics-heading">Inventory analytics</h2>
        </div>
        <strong className="metric-value">{analytics.activeCount}</strong>
      </div>
      <p className="muted">Active listings</p>
      <div className="analytics-grid">
        <AnalyticsList title="By category" values={analytics.listingsByCategory} />
        <AnalyticsList title="By status" values={analytics.listingsByStatus} />
        <div>
          <h3>Needs attention</h3>
          <p>{analytics.expiringListings.length} expiring soon</p>
          <p>{analytics.lowRemainingQuantityListings.length} low remaining quantity</p>
        </div>
      </div>
    </section>
  );
}

function AnalyticsList({
  title,
  values,
}: {
  title: string;
  values: InventoryAnalytics['listingsByCategory'];
}) {
  return (
    <div>
      <h3>{title}</h3>
      {values.length === 0 ? (
        <p className="muted">No data yet.</p>
      ) : (
        <ul className="summary-list">
          {values.map((value) => <li key={value.key}>{value.key}: {value.count}</li>)}
        </ul>
      )}
    </div>
  );
}

function messageFor(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Unable to load inventory analytics.';
}

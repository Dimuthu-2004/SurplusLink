import { AnalyticsChart, AnalyticsMetric } from '../../components/AnalyticsChart';
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

      </div>
      <div className="analytics-metrics">
        <AnalyticsMetric label="Active listings" value={analytics.activeCount} detail="Available inventory" />
        <AnalyticsMetric label="Total listings" value={analytics.listingsByStatus.reduce((sum, row) => sum + row.count, 0)} />
        <AnalyticsMetric label="Expiring soon" value={analytics.expiringListings.length} detail="Listings in the attention preview" />
        <AnalyticsMetric label="Low remaining quantity" value={analytics.lowRemainingQuantityListings.length} detail="Listings in the attention preview" />
      </div>
      <div className="analytics-grid">
        <AnalyticsChart title="Inventory by category" values={analytics.listingsByCategory} />
        <AnalyticsChart title="Inventory by status" values={analytics.listingsByStatus} initialView="ring" />
        <div>
          <h3>Needs attention</h3>
          <p>{analytics.expiringListings.length} expiring soon</p>
          <p>{analytics.lowRemainingQuantityListings.length} low remaining quantity</p>
        </div>
      </div>
    </section>
  );
}

function messageFor(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Unable to load inventory analytics.';
}

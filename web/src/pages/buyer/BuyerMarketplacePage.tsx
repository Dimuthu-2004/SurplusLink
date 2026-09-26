import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import {
  fetchMarketplaceListings,
  fetchMaterialCategories,
  type MaterialListingItem,
  type MaterialCategoryItem,
  type MarketplaceQueryParams,
} from '../../api/buyerMarketplaceApi';
import { BuyerListingCard } from './BuyerListingCard';
import './buyerMarketplace.css';

export function BuyerMarketplacePage() {
  const { user, logout } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  // Categories state
  const [categories, setCategories] = useState<MaterialCategoryItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>(
    searchParams.get('category') || '',
  );

  // Search state
  const [searchTerm, setSearchTerm] = useState<string>(searchParams.get('search') || '');
  const [debouncedSearch, setDebouncedSearch] = useState<string>(
    searchParams.get('search') || '',
  );

  // Filter state
  const [condition, setCondition] = useState<string>(searchParams.get('condition') || '');
  const [sortBy, setSortBy] = useState<MarketplaceQueryParams['sortBy']>(
    (searchParams.get('sortBy') as MarketplaceQueryParams['sortBy']) || 'createdAt',
  );
  const [sortDir, setSortDir] = useState<MarketplaceQueryParams['sortDir']>(
    (searchParams.get('sortDir') as MarketplaceQueryParams['sortDir']) || 'desc',
  );
  const [page, setPage] = useState<number>(parseInt(searchParams.get('page') || '1', 10) || 1);

  // Results state
  const [listings, setListings] = useState<MaterialListingItem[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());
  const [isRefreshing, setIsRefreshing] = useState<boolean>(false);

  // Debounce search input
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchTerm);
      setPage(1);
    }, 300);
    return () => clearTimeout(handler);
  }, [searchTerm]);

  // Load categories
  useEffect(() => {
    fetchMaterialCategories()
      .then((data) => setCategories(data))
      .catch(() => {
        // Fallback silently if categories fail to load
      });
  }, []);

  // Sync state to URL params
  useEffect(() => {
    const params = new URLSearchParams();
    if (selectedCategory) params.set('category', selectedCategory);
    if (debouncedSearch) params.set('search', debouncedSearch);
    if (condition) params.set('condition', condition);
    if (sortBy && sortBy !== 'createdAt') params.set('sortBy', sortBy);
    if (sortDir && sortDir !== 'desc') params.set('sortDir', sortDir);
    if (page > 1) params.set('page', page.toString());
    setSearchParams(params, { replace: true });
  }, [selectedCategory, debouncedSearch, condition, sortBy, sortDir, page, setSearchParams]);

  // Fetch listings
  const loadListings = useCallback(
    async (isBackground = false) => {
      if (!isBackground) {
        setLoading(true);
      } else {
        setIsRefreshing(true);
      }
      setError(null);

      try {
        const res = await fetchMarketplaceListings({
          category: selectedCategory || undefined,
          search: debouncedSearch || undefined,
          condition: condition || undefined,
          sortBy,
          sortDir,
          page,
          pageSize: 12,
        });

        setListings(res.items);
        setTotalCount(res.totalCount);
        setTotalPages(Math.max(1, res.totalPages));
        setLastUpdated(new Date());
      } catch (err: unknown) {
        if (!isBackground) {
          setError(err instanceof Error ? err.message : 'Failed to load materials.');
        }
      } finally {
        setLoading(false);
        setIsRefreshing(false);
      }
    },
    [selectedCategory, debouncedSearch, condition, sortBy, sortDir, page],
  );

  useEffect(() => {
    loadListings(false);
  }, [loadListings]);

  // Background refresh every 25 seconds & on window focus
  const loadRef = useRef(loadListings);
  loadRef.current = loadListings;

  useEffect(() => {
    const interval = setInterval(() => {
      loadRef.current(true);
    }, 25000);

    const onFocus = () => {
      loadRef.current(true);
    };
    window.addEventListener('focus', onFocus);

    return () => {
      clearInterval(interval);
      window.removeEventListener('focus', onFocus);
    };
  }, []);

  const handleCategorySelect = (categoryName: string) => {
    setSelectedCategory((prev) => (prev === categoryName ? '' : categoryName));
    setPage(1);
  };

  const handleClearFilters = () => {
    setSelectedCategory('');
    setSearchTerm('');
    setDebouncedSearch('');
    setCondition('');
    setSortBy('createdAt');
    setSortDir('desc');
    setPage(1);
  };

  return (
    <div className="marketplace-page-content">
      {/* Hero & Search Banner */}
      <section className="marketplace-hero-section">
        <div className="marketplace-hero-banner">
          <div className="marketplace-hero-content">
            <h1 className="sr-only">Buyer home</h1>
            <span className="marketplace-hero-tag">Construction Material Marketplace</span>
            <h2 className="marketplace-hero-title">Browse Verified Surplus Materials</h2>
            <p className="marketplace-hero-desc">
              Discover active surplus stock across Sri Lanka. Find the right materials and continue
              seamlessly in the SurplusLink Mobile App with AI matching.
            </p>

            <form
              className="marketplace-search-form"
              onSubmit={(e) => e.preventDefault()}
              role="search"
            >
              <svg
                className="marketplace-search-icon"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <circle cx="11" cy="11" r="8" />
                <line x1="21" y1="21" x2="16.65" y2="16.65" />
              </svg>
              <input
                type="text"
                placeholder="Search materials by name or description..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="marketplace-search-input"
                aria-label="Search materials"
              />
              {searchTerm && (
                <button
                  type="button"
                  className="marketplace-search-clear"
                  onClick={() => setSearchTerm('')}
                  aria-label="Clear search"
                >
                  &times;
                </button>
              )}
            </form>
          </div>
        </div>
      </section>

      {/* Dynamic Category Navigation Bar */}
      <section className="marketplace-category-bar" aria-label="Categories">
        <div className="category-chips-scroll">
          <button
            type="button"
            className={`category-chip-btn ${selectedCategory === '' ? 'active' : ''}`}
            onClick={() => handleCategorySelect('')}
          >
            All Materials
          </button>
          {categories.map((cat) => (
            <button
              key={cat.id}
              type="button"
              className={`category-chip-btn ${selectedCategory === cat.name ? 'active' : ''}`}
              onClick={() => handleCategorySelect(cat.name)}
            >
              {cat.name}
            </button>
          ))}
        </div>
      </section>

      {/* Main Content Area */}
      <main className="marketplace-main-container">
        {/* Controls & Filter Bar */}
        <div className="marketplace-controls-bar">
          <div className="marketplace-summary-count">
            Showing <strong>{totalCount}</strong> {totalCount === 1 ? 'material' : 'materials'}
            {selectedCategory && ` in ${selectedCategory}`}
            <span className="marketplace-live-badge">
              <span className="live-pulse-dot" />
              {isRefreshing ? 'Refreshing...' : 'Updated just now'}
            </span>
          </div>

          <div className="marketplace-filters-group">
            {/* Condition Filter */}
            <select
              className="marketplace-select"
              value={condition}
              onChange={(e) => {
                setCondition(e.target.value);
                setPage(1);
              }}
              aria-label="Filter by condition"
            >
              <option value="">All Conditions</option>
              <option value="NEW">New</option>
              <option value="EXCELLENT">Excellent</option>
              <option value="GOOD">Good</option>
              <option value="FAIR">Fair</option>
              <option value="POOR">Poor</option>
            </select>

            {/* Sort Filter */}
            <select
              className="marketplace-select"
              value={`${sortBy}_${sortDir}`}
              onChange={(e) => {
                const [sb, sd] = e.target.value.split('_');
                setSortBy(sb as MarketplaceQueryParams['sortBy']);
                setSortDir(sd as MarketplaceQueryParams['sortDir']);
                setPage(1);
              }}
              aria-label="Sort listings"
            >
              <option value="createdAt_desc">Newest Listings</option>
              <option value="unitPrice_asc">Price: Low to High</option>
              <option value="unitPrice_desc">Price: High to Low</option>
              <option value="quantity_desc">Quantity: High to Low</option>
            </select>

            {(selectedCategory || debouncedSearch || condition) && (
              <button
                type="button"
                className="button button-secondary"
                style={{ padding: '0.4rem 0.85rem', fontSize: '0.84rem' }}
                onClick={handleClearFilters}
              >
                Clear Filters
              </button>
            )}
          </div>
        </div>

        {/* Listings Display */}
        {loading ? (
          <div style={{ textAlign: 'center', padding: '5rem 0' }}>
            <div className="spinner" style={{ margin: '0 auto 1.25rem' }} />
            <p style={{ color: 'var(--sl-market-text-muted)', fontWeight: 600 }}>
              Finding available materials...
            </p>
          </div>
        ) : error ? (
          <div className="marketplace-empty-state">
            <h2 className="empty-state-title">Unable to Load Listings</h2>
            <p className="empty-state-text">{error}</p>
            <button
              type="button"
              className="button button-primary"
              onClick={() => loadListings(false)}
            >
              Try Again
            </button>
          </div>
        ) : listings.length === 0 ? (
          <div className="marketplace-empty-state">
            <svg
              className="empty-state-icon"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
            >
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <h2 className="empty-state-title">No Materials Found</h2>
            <p className="empty-state-text">
              We couldn't find any surplus materials matching your current filters. Try selecting a
              different category or clearing your search.
            </p>
            <button
              type="button"
              className="button button-primary"
              onClick={handleClearFilters}
            >
              Reset All Filters
            </button>
          </div>
        ) : (
          <div className="marketplace-grid" data-testid="marketplace-grid">
            {listings.map((item) => (
              <BuyerListingCard key={item.id} listing={item} />
            ))}
          </div>
        )}

        {/* Pagination */}
        {!loading && totalPages > 1 && (
          <nav className="marketplace-pagination" aria-label="Pagination">
            <button
              type="button"
              className="pagination-btn"
              disabled={page <= 1}
              onClick={() => {
                setPage((p) => Math.max(1, p - 1));
                window.scrollTo({ top: 0, behavior: 'smooth' });
              }}
              aria-label="Previous page"
            >
              &larr; Previous
            </button>

            <span className="pagination-info">
              Page {page} of {totalPages}
            </span>

            <button
              type="button"
              className="pagination-btn"
              disabled={page >= totalPages}
              onClick={() => {
                setPage((p) => Math.min(totalPages, p + 1));
                window.scrollTo({ top: 0, behavior: 'smooth' });
              }}
              aria-label="Next page"
            >
              Next &rarr;
            </button>
          </nav>
        )}
      </main>
    </div>
  );
}

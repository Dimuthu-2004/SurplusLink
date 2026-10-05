import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { fetchItemTemplates, type ConstructionItemTemplate } from '../../api/constructionItemTemplatesApi';
import { fetchMarketplaceListings, fetchMaterialCategories, type MaterialCategoryItem, type MaterialListingItem, type MarketplaceQueryParams } from '../../api/buyerMarketplaceApi';
import { BuyerListingCard } from './BuyerListingCard';
import { MobileHandoffModal } from './MobileHandoffModal';
import './buyerMarketplace.css';

type CategoryPreview = { count: number; image?: string };

export function BuyerMarketplacePage() {
  const [params, setParams] = useSearchParams();
  const [categories, setCategories] = useState<MaterialCategoryItem[]>([]);
  const [templates, setTemplates] = useState<ConstructionItemTemplate[]>([]);
  const [previews, setPreviews] = useState<Record<string, CategoryPreview>>({});
  const [categoryId, setCategoryId] = useState(params.get('category') || '');
  const [templateId, setTemplateId] = useState(params.get('template') || '');
  const [search, setSearch] = useState(params.get('search') || '');
  const [condition, setCondition] = useState(params.get('condition') || '');
  const [sortBy, setSortBy] = useState<MarketplaceQueryParams['sortBy']>('createdAt');
  const [sortDir, setSortDir] = useState<MarketplaceQueryParams['sortDir']>('desc');
  const [page, setPage] = useState(1);
  const [listings, setListings] = useState<MaterialListingItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPrompt, setShowPrompt] = useState(false);
  const [showQr, setShowQr] = useState(false);
  const promptShown = useRef(false);

  useEffect(() => {
    void Promise.all([
      fetchMaterialCategories(),
      fetchItemTemplates().catch(() => [] as ConstructionItemTemplate[]),
    ])
      .then(([loadedCategories, loadedTemplates]) => {
        setCategories(loadedCategories);
        setTemplates((loadedTemplates || []).filter((item) => item.isActive));
        void Promise.all(
          loadedCategories.map(async (category) => {
            const result = await fetchMarketplaceListings({ category: category.id, pageSize: 1 });
            return [category.id, { count: result.totalCount, image: result.items[0]?.photos?.[0]?.photoUrl }] as const;
          })
        )
          .then((entries) => setPreviews(Object.fromEntries(entries)))
          .catch(() => undefined);
      })
      .catch(() => setError('Unable to load the marketplace.'));
  }, []);

  const selectedCategory = categories.find((c) => c.id === categoryId || c.name === categoryId);
  const selectedTemplate = templates.find((t) => t.id === templateId);
  const categoryTemplates = templates.filter((t) => t.categoryId === selectedCategory?.id);

  useEffect(() => {
    const next = new URLSearchParams();
    if (categoryId) next.set('category', categoryId);
    if (templateId) next.set('template', templateId);
    if (search) next.set('search', search);
    if (condition) next.set('condition', condition);
    if (page > 1) next.set('page', String(page));
    setParams(next, { replace: true });
  }, [categoryId, condition, page, search, setParams, templateId]);

  const loadListings = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await fetchMarketplaceListings({
        category: categoryId || undefined,
        templateId: templateId || undefined,
        search: search || undefined,
        condition: condition || undefined,
        sortBy,
        sortDir,
        page,
        pageSize: 12,
      });
      setListings(result.items);
      setTotalCount(result.totalCount);
      setTotalPages(Math.max(1, result.totalPages));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load active listings.');
    } finally {
      setLoading(false);
    }
  }, [categoryId, condition, page, search, sortBy, sortDir, templateId]);

  useEffect(() => {
    void loadListings();
  }, [loadListings]);

  useEffect(() => {
    const onScroll = () => {
      const max = document.documentElement.scrollHeight - window.innerHeight;
      if (!promptShown.current && max > 400 && window.scrollY / max > 0.35) {
        promptShown.current = true;
        setShowPrompt(true);
      }
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  const chooseCategory = (nameOrId: string) => {
    setCategoryId(nameOrId);
    setTemplateId('');
    setPage(1);
    promptShown.current = false;
  };

  const backToCategories = () => {
    setCategoryId('');
    setTemplateId('');
    setSearch('');
    setCondition('');
    setPage(1);
    setShowPrompt(false);
  };

  const handoffCategory = selectedCategory ?? categories.find((c) => c.id === selectedTemplate?.categoryId);

  return (
    <div className="marketplace-page-content">
      <h1 className="sr-only">Buyer home</h1>

      <section className="marketplace-discovery-header">
        <p className="marketplace-kicker">Marketplace</p>
        <h1>{selectedTemplate?.name ?? selectedCategory?.name ?? 'Marketplace'}</h1>
        <p>Find surplus construction materials</p>
        <label className="marketplace-discovery-search">
          <span aria-hidden="true">⌕</span>
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Search materials..."
            aria-label="Search materials"
          />
        </label>
      </section>

      {/* Category Navigation Bar / Chips */}
      <section className="marketplace-category-bar" aria-label="Categories">
        <div className="category-chips-scroll">
          <button
            type="button"
            className={`category-chip-btn ${!categoryId ? 'active' : ''}`}
            onClick={() => chooseCategory('')}
          >
            All Materials
          </button>
          {categories.map((cat) => (
            <button
              key={cat.id}
              type="button"
              className={`category-chip-btn ${categoryId === cat.name || categoryId === cat.id ? 'active' : ''}`}
              onClick={() => chooseCategory(cat.name)}
            >
              {cat.name}
            </button>
          ))}
        </div>
      </section>

      {categoryTemplates.length > 0 && (
        <nav className="marketplace-template-filter" aria-label="Catalog item filter">
          <button className={!templateId ? 'active' : ''} onClick={() => { setTemplateId(''); setPage(1); }}>
            All
          </button>
          {categoryTemplates.map((template) => (
            <button
              key={template.id}
              className={templateId === template.id ? 'active' : ''}
              onClick={() => { setTemplateId(template.id); setPage(1); }}
            >
              {template.name}
            </button>
          ))}
        </nav>
      )}

      {/* Controls & Filter Bar */}
      <div className="marketplace-controls-bar">
        <span>Filters</span>
        <select
          value={condition}
          onChange={(e) => {
            setCondition(e.target.value);
            setPage(1);
          }}
          aria-label="All conditions"
        >
          <option value="">All conditions</option>
          <option value="NEW">New</option>
          <option value="EXCELLENT">Excellent</option>
          <option value="GOOD">Good</option>
          <option value="FAIR">Fair</option>
        </select>
        <select
          value={`${sortBy}_${sortDir}`}
          onChange={(e) => {
            const [sort, direction] = e.target.value.split('_');
            setSortBy(sort as MarketplaceQueryParams['sortBy']);
            setSortDir(direction as MarketplaceQueryParams['sortDir']);
            setPage(1);
          }}
        >
          <option value="createdAt_desc">Newest listings</option>
          <option value="unitPrice_asc">Price: low to high</option>
          <option value="unitPrice_desc">Price: high to low</option>
        </select>
        <button type="button" className="button button-secondary" onClick={() => setShowQr(true)}>
          Find my best match with AI
        </button>
      </div>

      {loading ? (
        <p>Loading...</p>
      ) : error ? (
        <p className="field-error">{error}</p>
      ) : listings.length ? (
        <div className="marketplace-grid" data-testid="marketplace-grid">
          {listings.map((listing) => (
            <BuyerListingCard key={listing.id} listing={listing} />
          ))}
        </div>
      ) : (
        <div className="marketplace-empty-state">
          <h2>No active listings found.</h2>
        </div>
      )}

      {totalPages > 1 && (
        <nav className="marketplace-pagination" aria-label="Pagination">
          <button disabled={page <= 1} onClick={() => setPage((v) => v - 1)}>
            Previous
          </button>
          <span>
            {page} / {totalPages}
          </span>
          <button disabled={page >= totalPages} onClick={() => setPage((v) => v + 1)}>
            Next
          </button>
        </nav>
      )}

      {showPrompt && (
        <div className="marketplace-ai-prompt" role="dialog" aria-modal="true">
          <div>
            <h2>Want the best match for your requirement?</h2>
            <p>SurplusLink AI compares available materials using your required quantity, budget, location and delivery needs.</p>
            <button className="button button-secondary" onClick={() => setShowPrompt(false)}>
              Not now
            </button>
            <button
              className="button button-primary"
              onClick={() => {
                setShowPrompt(false);
                setShowQr(true);
              }}
            >
              Show QR code
            </button>
          </div>
        </div>
      )}

      {handoffCategory && (
        <MobileHandoffModal
          isOpen={showQr}
          onClose={() => setShowQr(false)}
          categoryId={handoffCategory.id}
          categoryName={selectedTemplate?.name ?? handoffCategory.name}
        />
      )}
    </div>
  );
}

function CategoryVisual({ image, name }: { image?: string; name: string }) {
  return image ? (
    <img src={image} alt="" className="marketplace-category-image" />
  ) : (
    <div className="marketplace-category-fallback" aria-hidden="true">
      {name.toLowerCase().includes('tool') ? '🔧' : name.toLowerCase().includes('roof') ? '🏠' : name.toLowerCase().includes('elect') ? '⚡' : '▦'}
    </div>
  );
}

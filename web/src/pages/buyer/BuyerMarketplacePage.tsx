import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { fetchItemTemplates, type ConstructionItemTemplate } from '../../api/constructionItemTemplatesApi';
import { fetchMarketplaceListings, fetchMaterialCategories, type MaterialCategoryItem, type MaterialListingItem, type MarketplaceQueryParams } from '../../api/buyerMarketplaceApi';
import { catalogLabel, useLanguage } from '../../i18n/LanguageContext';
import { BuyerListingCard } from './BuyerListingCard';
import { MobileHandoffModal } from './MobileHandoffModal';
import './buyerMarketplace.css';

type CategoryPreview = { count: number; image?: string };

export function BuyerMarketplacePage() {
  const { language, t } = useLanguage();
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
  const listingView = Boolean(categoryId || templateId || search);

  useEffect(() => {
    void Promise.all([fetchMaterialCategories(), fetchItemTemplates()]).then(([loadedCategories, loadedTemplates]) => {
      setCategories(loadedCategories);
      setTemplates(loadedTemplates.filter(item => item.isActive));
      // Counts and card imagery come from the real, buyer-visible active listings.
      void Promise.all(loadedCategories.map(async category => {
        const result = await fetchMarketplaceListings({ category: category.id, pageSize: 1 });
        return [category.id, { count: result.totalCount, image: result.items[0]?.photos?.[0]?.photoUrl }] as const;
      })).then(entries => setPreviews(Object.fromEntries(entries))).catch(() => undefined);
    }).catch(() => setError('Unable to load the marketplace.'));
  }, []);

  const selectedCategory = categories.find(category => category.id === categoryId);
  const selectedTemplate = templates.find(template => template.id === templateId);
  const categoryTemplates = templates.filter(template => template.categoryId === categoryId);

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
    if (!listingView) return;
    setLoading(true); setError(null);
    try {
      const result = await fetchMarketplaceListings({ category: categoryId || undefined, templateId: templateId || undefined, search: search || undefined, condition: condition || undefined, sortBy, sortDir, page, pageSize: 12 });
      setListings(result.items); setTotalCount(result.totalCount); setTotalPages(Math.max(1, result.totalPages));
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to load active listings.'); }
    finally { setLoading(false); }
  }, [categoryId, condition, listingView, page, search, sortBy, sortDir, templateId]);
  useEffect(() => { void loadListings(); }, [loadListings]);

  useEffect(() => {
    if (!listingView) return;
    const onScroll = () => {
      const max = document.documentElement.scrollHeight - window.innerHeight;
      if (!promptShown.current && max > 400 && window.scrollY / max > .35) { promptShown.current = true; setShowPrompt(true); }
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, [listingView, categoryId, templateId]);

  const chooseCategory = (id: string) => { setCategoryId(id); setTemplateId(''); setPage(1); promptShown.current = false; };
  const backToCategories = () => { setCategoryId(''); setTemplateId(''); setSearch(''); setCondition(''); setPage(1); setShowPrompt(false); };
  const handoffCategory = selectedCategory ?? categories.find(category => category.id === selectedTemplate?.categoryId);

  if (!listingView) return <div className="marketplace-page-content category-discovery">
    <section className="marketplace-discovery-header"><p className="marketplace-kicker">{t('marketplace')}</p><h1>{t('marketplace')}</h1><p>{t('findMaterials')}</p><label className="marketplace-discovery-search"><span aria-hidden="true">⌕</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder={t('searchMaterials')} aria-label={t('searchMaterials')} /></label></section>
    <section aria-labelledby="browse-categories"><h2 id="browse-categories">{t('browseCategories')}</h2><div className="marketplace-category-cards">
      {categories.filter(category => (previews[category.id]?.count ?? 0) > 0).map((category, index) => <button key={category.id} type="button" className="marketplace-category-card" style={{ animationDelay: `${index * 55}ms` }} onClick={() => chooseCategory(category.id)}>
        <CategoryVisual image={previews[category.id]?.image} name={category.name} />
        <span className="marketplace-category-card-body"><strong>{catalogLabel(category.name, language)}</strong><small>{previews[category.id]?.count ?? 0} {t('activeListings')}</small><span aria-hidden="true">→</span></span>
      </button>)}
    </div></section>
    {error && <p className="field-error">{error}</p>}
  </div>;

  return <div className="marketplace-page-content">
    <header className="marketplace-listing-header"><button type="button" className="back-link" onClick={backToCategories}>← {t('backToCategories')}</button><h1>{selectedTemplate ? catalogLabel(selectedTemplate.name, language) : selectedCategory ? catalogLabel(selectedCategory.name, language) : t('marketplace')}</h1><p>{totalCount} {t('activeListings')}</p><label className="marketplace-discovery-search"><span aria-hidden="true">⌕</span><input value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} placeholder={t('searchMaterials')} aria-label={t('searchMaterials')} /></label></header>
    {categoryTemplates.length > 0 && <nav className="marketplace-template-filter" aria-label="Catalog item filter"><button className={!templateId ? 'active' : ''} onClick={() => { setTemplateId(''); setPage(1); }}>All</button>{categoryTemplates.map(template => <button key={template.id} className={templateId === template.id ? 'active' : ''} onClick={() => { setTemplateId(template.id); setPage(1); }}>{catalogLabel(template.name, language)}</button>)}</nav>}
    <div className="marketplace-controls-bar"><span>{t('filters')}</span><select value={condition} onChange={event => { setCondition(event.target.value); setPage(1); }} aria-label={t('allConditions')}><option value="">{t('allConditions')}</option><option value="NEW">New</option><option value="EXCELLENT">Excellent</option><option value="GOOD">Good</option><option value="FAIR">Fair</option></select><select value={`${sortBy}_${sortDir}`} onChange={event => { const [sort, direction] = event.target.value.split('_'); setSortBy(sort as MarketplaceQueryParams['sortBy']); setSortDir(direction as MarketplaceQueryParams['sortDir']); setPage(1); }}><option value="createdAt_desc">{t('sortNewest')}</option><option value="unitPrice_asc">Price: low to high</option><option value="unitPrice_desc">Price: high to low</option></select><button type="button" className="button button-secondary" onClick={() => setShowQr(true)}>{t('findBestMatch')}</button></div>
    {loading ? <p>{t('loading')}</p> : error ? <p className="field-error">{error}</p> : listings.length ? <div className="marketplace-grid">{listings.map(listing => <BuyerListingCard key={listing.id} listing={listing} />)}</div> : <div className="marketplace-empty-state"><h2>{t('noListings')}</h2></div>}
    {totalPages > 1 && <nav className="marketplace-pagination"><button disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Previous</button><span>{page} / {totalPages}</span><button disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next</button></nav>}
    {showPrompt && <div className="marketplace-ai-prompt" role="dialog" aria-modal="true"><div><h2>{t('handoffTitle')}</h2><p>{t('handoffDescription')}</p><button className="button button-secondary" onClick={() => setShowPrompt(false)}>{t('notNow')}</button><button className="button button-primary" onClick={() => { setShowPrompt(false); setShowQr(true); }}>{t('continueQr')}</button></div></div>}
    {handoffCategory && <MobileHandoffModal isOpen={showQr} onClose={() => setShowQr(false)} categoryId={handoffCategory.id} categoryName={catalogLabel(selectedTemplate?.name ?? handoffCategory.name, language)} />}
  </div>;
}

function CategoryVisual({ image, name }: { image?: string; name: string }) {
  return image ? <img src={image} alt="" className="marketplace-category-image" /> : <div className="marketplace-category-fallback" aria-hidden="true">{name.toLowerCase().includes('tool') ? '🔧' : name.toLowerCase().includes('roof') ? '🏠' : name.toLowerCase().includes('elect') ? '⚡' : '▦'}</div>;
}

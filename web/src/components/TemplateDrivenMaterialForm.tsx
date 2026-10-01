import { FormEvent, useMemo, useRef, useState } from 'react';
import type {
  ConstructionItemTemplate,
  AttributeFieldDefinition,
} from '../api/constructionItemTemplatesApi';
import type { MaterialCategory } from '../features/materials/managerMaterialsApi';
import { ConstructionItemPicker } from './ConstructionItemPicker';
import { localized, useLanguage } from '../i18n/LanguageContext';
import { ApiError } from '../api/apiClient';

export interface ListingSubmitData {
  title: string;
  categoryId: string;
  condition: string;
  description: string;
  location: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  quantityMode: string;
  packageType?: string | null;
  packageSize?: number | null;
  packageCount?: number | null;
  pricePerPackage?: number | null;
  constructionItemTemplateId?: string | null;
  specificationsJson?: string | null;
  isCustomPendingReview?: boolean;
}

export interface TemplateDrivenMaterialFormProps {
  templates: ConstructionItemTemplate[];
  categories: MaterialCategory[];
  onSubmit: (data: ListingSubmitData) => Promise<void>;
  initialData?: Partial<ListingSubmitData>;
  onCancel?: () => void;
}

export function TemplateDrivenMaterialForm({
  templates,
  categories,
  onSubmit,
  initialData,
  onCancel,
}: TemplateDrivenMaterialFormProps) {
  const { language, t } = useLanguage();
  const fieldRefs = useRef<Record<string, HTMLElement | null>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const reportFieldErrors = (next: Record<string, string>) => {
    setFieldErrors(next);
    const first = Object.keys(next)[0];
    if (first) window.setTimeout(() => fieldRefs.current[first]?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 0);
    if (first) window.setTimeout(() => fieldRefs.current[first]?.focus(), 250);
  };
  // Template selection
  const [selectedTemplate, setSelectedTemplate] = useState<ConstructionItemTemplate | null>(() => {
    if (initialData?.constructionItemTemplateId) {
      return templates.find((t) => t.id === initialData.constructionItemTemplateId) || null;
    }
    return null;
  });
  const [isCustomSelected, setIsCustomSelected] = useState<boolean>(
    Boolean(initialData?.isCustomPendingReview && !initialData?.constructionItemTemplateId)
  );

  // Common listing fields
  const [title, setTitle] = useState(initialData?.title || '');
  const [categoryId, setCategoryId] = useState(initialData?.categoryId || '');
  const [condition, setCondition] = useState(initialData?.condition || 'NEW');
  const [description, setDescription] = useState(initialData?.description || '');
  const [location, setLocation] = useState(initialData?.location || 'Colombo');

  // Dynamic specs map
  const [specs, setSpecs] = useState<Record<string, string | number>>(() => {
    if (initialData?.specificationsJson) {
      try {
        return JSON.parse(initialData.specificationsJson);
      } catch {
        return {};
      }
    }
    return {};
  });

  // Quantity & Packaging inputs (seller-friendly, no jargon)
  // For PACKAGE mode:
  const [packageSize, setPackageSize] = useState<number>(initialData?.packageSize || 1);
  const [packageCount, setPackageCount] = useState<number>(initialData?.packageCount || 1);
  const [pricePerPackage, setPricePerPackage] = useState<number>(
    initialData?.pricePerPackage || initialData?.unitPrice || 0
  );

  // For PIECE mode:
  const [pieceCount, setPieceCount] = useState<number>(initialData?.quantity || 1);
  const [pricePerPiece, setPricePerPiece] = useState<number>(initialData?.unitPrice || 0);

  // For CONTINUOUS_BULK mode:
  const [bulkQuantity, setBulkQuantity] = useState<number>(initialData?.quantity || 1);
  const [selectedUnit, setSelectedUnit] = useState<string>(initialData?.unit || 'kg');
  const [pricePerBulkUnit, setPricePerBulkUnit] = useState<number>(initialData?.unitPrice || 0);

  // Custom Item sale format (friendly seller options)
  const [customSaleType, setCustomSaleType] = useState<'PIECE' | 'PACKAGE' | 'BULK'>('PIECE');
  const [customPackageTypeName, setCustomPackageTypeName] = useState('box');
  const [customUnit, setCustomUnit] = useState('unit');

  // Tile dimensions calculator state (Section 4 & 9)
  const packageSource = selectedTemplate ? (JSON.parse(selectedTemplate.attributeSchema || '[]') as AttributeFieldDefinition[]).find(f => f.packageSizeSource && f.packageSizeSource !== 'QUANTITY_FIELD') : undefined;
  const isTileTemplate = packageSource?.packageSizeSource === 'CALCULATED';
  const [tileWidth, setTileWidth] = useState<number>(600);
  const [tileHeight, setTileHeight] = useState<number>(600);
  const [tilesPerBox, setTilesPerBox] = useState<number>(4);

  const handleTileDimChange = (w: number, h: number, pcs: number) => {
    setTileWidth(w);
    setTileHeight(h);
    setTilesPerBox(pcs);
    if (w > 0 && h > 0 && pcs > 0) {
      const cov = (w * h * pcs) / 1000000;
      setPackageSize(cov);
      setSpecs((prev) => ({
        ...prev,
        widthMm: w,
        heightMm: h,
        piecesPerBox: pcs,
        coveragePerBoxSqm: cov,
        dimensionsMm: `${w}x${h} mm`,
      }));
    }
  };

  // Form submission & feedback
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Parse template attribute schema
  const attributeFields = useMemo<AttributeFieldDefinition[]>(() => {
    if (!selectedTemplate?.attributeSchema) return [];
    try {
      const parsed = JSON.parse(selectedTemplate.attributeSchema);
      return Array.isArray(parsed) ? (parsed as AttributeFieldDefinition[]) : [];
    } catch {
      return [];
    }
  }, [selectedTemplate]);

  // When a template is chosen, prefill sensible defaults
  const handleSelectTemplate = (template: ConstructionItemTemplate) => {
    setSelectedTemplate(template);
    setIsCustomSelected(false);
    setTitle(template.name);
    setCategoryId(template.categoryId);
    const sizeSource = (JSON.parse(template.attributeSchema || '[]') as AttributeFieldDefinition[]).find(f => f.packageSizeSource);
    setSpecs(sizeSource?.packageSizeSource === 'CALCULATED'
      ? { widthMm: tileWidth, heightMm: tileHeight, piecesPerBox: tilesPerBox,
          coveragePerBoxSqm: tileWidth * tileHeight * tilesPerBox / 1000000 } : {});

    // Default package size if package mode
    if (template.quantityMode === 'PACKAGE') {
      const source = (JSON.parse(template.attributeSchema || "[]") as AttributeFieldDefinition[]).find(f => f.packageSizeSource);
      const defaultSize = source?.packageSizeSource === "SPECIFICATION_FIELD" ? 0 : template.allowedPackageSizes?.[0] || 1;
      setPackageSize(defaultSize);
    }
    if (template.allowedUnits?.length > 0) {
      setSelectedUnit(template.allowedUnits[0]);
    } else {
      setSelectedUnit(template.baseUnit);
    }
  };

  const handleSelectCustom = () => {
    setSelectedTemplate(null);
    setIsCustomSelected(true);
    if (!title || templates.some((t) => t.name === title)) {
      setTitle('');
    }
    if (categories.length > 0 && !categoryId) {
      setCategoryId(categories[0].id);
    }
  };

  // Determine current effective quantity mode
  const effectiveMode = useMemo(() => {
    if (selectedTemplate) {
      if (selectedTemplate.quantityMode === 'PACKAGE') return 'PACKAGE';
      if (selectedTemplate.quantityMode === 'PIECE') return 'PIECE';
      return 'CONTINUOUS';
    }
    if (isCustomSelected) {
      if (customSaleType === 'PACKAGE') return 'PACKAGE';
      if (customSaleType === 'PIECE') return 'PIECE';
      return 'CONTINUOUS';
    }
    return 'PIECE';
  }, [selectedTemplate, isCustomSelected, customSaleType]);

  // Calculated quantities for preview
  const calculatedTotalQuantity = useMemo(() => {
    if (effectiveMode === 'PACKAGE') {
      return (packageSize || 0) * (packageCount || 0);
    }
    if (effectiveMode === 'PIECE') {
      return pieceCount || 0;
    }
    return bulkQuantity || 0;
  }, [effectiveMode, packageSize, packageCount, pieceCount, bulkQuantity]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});

    if (!selectedTemplate && !isCustomSelected) {
      setError('Please choose what item you are listing, or select "Can\'t find your item".');
      return;
    }
    if (!title.trim()) {
      reportFieldErrors({ title: t('required') });
      return;
    }
    if (!categoryId) {
      reportFieldErrors({ categoryId: t('required') });
      return;
    }

    let finalQuantity = 0;
    let finalUnit = 'unit';
    let finalUnitPrice = 0;
    let finalPackageType: string | null = null;
    let finalPackageSize: number | null = null;
    let finalPackageCount: number | null = null;
    let finalPricePerPackage: number | null = null;

    if (effectiveMode === 'PACKAGE') {
      if (packageCount <= 0 || packageSize <= 0) {
        reportFieldErrors({ ...(packageSize <= 0 ? { packageSize: t('quantityRequired') } : {}), ...(packageCount <= 0 ? { packageCount: t('packageCountRequired') } : {}) });
        return;
      }
      if (pricePerPackage <= 0) {
        reportFieldErrors({ pricePerPackage: t('priceRequired') });
        return;
      }
      if (!Number.isInteger(packageCount)) { reportFieldErrors({ packageCount: "Enter a whole number of packages." }); return; }
      finalPackageCount = packageCount;
      finalPackageSize = packageSize;
      finalQuantity = finalPackageCount * finalPackageSize;
      finalUnit = selectedTemplate?.baseUnit || customUnit || 'unit';
      finalPackageType = (selectedTemplate?.packageType || customPackageTypeName || 'OTHER').toUpperCase();
      finalPricePerPackage = pricePerPackage;
      // `unitPrice` is the physical package price for PACKAGE mode.  Quantity
      // remains base-equivalent solely for compatibility/matching.
      finalUnitPrice = pricePerPackage;
    } else if (effectiveMode === 'PIECE') {
      if (pieceCount <= 0 || pricePerPiece <= 0) {
        reportFieldErrors({ ...(pieceCount <= 0 ? { quantity: t('quantityRequired') } : {}), ...(pricePerPiece <= 0 ? { unitPrice: t('priceRequired') } : {}) });
        return;
      }
      finalQuantity = Math.floor(pieceCount);
      finalUnit = selectedTemplate?.baseUnit || 'unit';
      finalUnitPrice = pricePerPiece;
      finalPackageType = null;
      finalPackageSize = 1;
      finalPackageCount = finalQuantity;
    } else {
      // CONTINUOUS / BULK
      if (bulkQuantity <= 0 || pricePerBulkUnit <= 0) {
        reportFieldErrors({ ...(bulkQuantity <= 0 ? { quantity: t('quantityRequired') } : {}), ...(pricePerBulkUnit <= 0 ? { unitPrice: t('priceRequired') } : {}) });
        return;
      }
      finalQuantity = bulkQuantity;
      finalUnit = selectedUnit || selectedTemplate?.baseUnit || customUnit || 'kg';
      finalUnitPrice = pricePerBulkUnit;
      finalPackageType = null;
      finalPackageSize = null;
      finalPackageCount = null;
    }

    const payload: ListingSubmitData = {
      title: title.trim(),
      categoryId,
      condition,
      description: description.trim(),
      location: location.trim(),
      quantity: finalQuantity,
      unit: finalUnit,
      unitPrice: finalUnitPrice,
      quantityMode: effectiveMode === 'PACKAGE' ? 'PACKAGE' : effectiveMode === 'PIECE' ? 'PIECE' : 'CONTINUOUS',
      packageType: finalPackageType,
      packageSize: finalPackageSize,
      packageCount: finalPackageCount,
      pricePerPackage: finalPricePerPackage,
      constructionItemTemplateId: selectedTemplate?.id || null,
      specificationsJson: Object.keys(specs).length > 0 ? JSON.stringify(specs) : null,
      isCustomPendingReview: isCustomSelected,
    };

    setIsSubmitting(true);
    try {
      await onSubmit(payload);
    } catch (err: unknown) {
      if (err instanceof ApiError && err.validationErrors) {
        const normalized = Object.fromEntries(Object.entries(err.validationErrors).map(([field, messages]) => [normalizeField(field), localizedValidation(messages[0], field, t)]));
        if (Object.keys(normalized).length) { reportFieldErrors(normalized); return; }
      }
      setError(err instanceof Error ? err.message : 'Failed to submit listing.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="template-driven-form" data-testid="template-driven-form" style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      {error && <div className="error-banner" data-testid="form-error-banner">{error}</div>}

      {/* Step 1: Item Picker */}
      <ConstructionItemPicker
        templates={templates}
        selectedTemplate={selectedTemplate}
        onSelectTemplate={handleSelectTemplate}
        onSelectCustom={handleSelectCustom}
        isCustomSelected={isCustomSelected}
      />

      {/* Step 2: Item Details & Dynamic Fields */}
      {(selectedTemplate || isCustomSelected) && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1rem' }}>
            <div className={`catalog-field ${fieldErrors.title ? 'has-error' : ''}`}>
              <label>Listing Title *</label>
              <input
                type="text"
                required
                placeholder="e.g. Dulux Weathershield Brilliant White 20L"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                data-testid="input-listing-title"
                ref={element => { fieldRefs.current.title = element; }}
                aria-invalid={Boolean(fieldErrors.title)}
              />
              {fieldErrors.title && <small className="field-error">{fieldErrors.title}</small>}
            </div>

            {isCustomSelected && (
              <div className={`catalog-field ${fieldErrors.categoryId ? 'has-error' : ''}`}>
                <label>Category *</label>
                <select
                  required
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  data-testid="select-custom-category"
                  ref={element => { fieldRefs.current.categoryId = element; }}
                  aria-invalid={Boolean(fieldErrors.categoryId)}
                >
                  <option value="">Select Category</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
                {fieldErrors.categoryId && <small className="field-error">{fieldErrors.categoryId}</small>}
              </div>
            )}

            <div className="catalog-field">
              <label>Condition *</label>
              <select
                value={condition}
                onChange={(e) => setCondition(e.target.value)}
                data-testid="select-listing-condition"
              >
                <option value="NEW">Brand New / Unused</option>
                <option value="EXCELLENT">Like New</option>
                <option value="GOOD">Good / Operational</option>
                <option value="FAIR">Fair / Functional</option>
              </select>
            </div>
          </div>

          {/* Dynamic Template Attributes (e.g. Paint: Colour, Finish; Generator: Capacity, Fuel, Running Hours) */}
          {attributeFields.length > 0 && (
            <div
              style={{
                padding: '1.2rem',
                background: '#f8fafc',
                border: '1px solid #e2e8f0',
                borderRadius: '10px',
                display: 'flex',
                flexDirection: 'column',
                gap: '1rem',
              }}
              data-testid="dynamic-specifications-section"
            >
              <div style={{ fontWeight: 650, fontSize: '0.95rem', color: '#1e293b' }}>
                {selectedTemplate?.name} Specifications
              </div>

              {isTileTemplate && (
                <div
                  style={{
                    background: '#f0fdf4',
                    border: '1px solid #bbf7d0',
                    borderRadius: '8px',
                    padding: '0.9rem',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '0.65rem',
                  }}
                  data-testid="tile-dimensions-calculator"
                >
                  <div style={{ fontWeight: 650, fontSize: '0.85rem', color: '#166534' }}>
                    Structured Tile Dimensions & Coverage Calculator
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))', gap: '0.75rem' }}>
                    <div className="catalog-field">
                      <label style={{ fontSize: '0.8rem' }}>Tile Width (mm) *</label>
                      <input
                        type="number"
                        placeholder="600"
                        value={tileWidth || ''}
                        onChange={(e) => handleTileDimChange(parseFloat(e.target.value) || 0, tileHeight, tilesPerBox)}
                      />
                    </div>
                    <div className="catalog-field">
                      <label style={{ fontSize: '0.8rem' }}>Tile Height (mm) *</label>
                      <input
                        type="number"
                        placeholder="600"
                        value={tileHeight || ''}
                        onChange={(e) => handleTileDimChange(tileWidth, parseFloat(e.target.value) || 0, tilesPerBox)}
                      />
                    </div>
                    <div className="catalog-field">
                      <label style={{ fontSize: '0.8rem' }}>Pieces per Box *</label>
                      <input
                        type="number"
                        placeholder="4"
                        value={tilesPerBox || ''}
                        onChange={(e) => handleTileDimChange(tileWidth, tileHeight, parseInt(e.target.value, 10) || 0)}
                      />
                    </div>
                  </div>
                  {tileWidth > 0 && tileHeight > 0 && tilesPerBox > 0 && (
                    <div style={{ fontSize: '0.82rem', color: '#15803d', fontWeight: 600 }}>
                      ✓ Each box covers {((tileWidth * tileHeight * tilesPerBox) / 1000000).toFixed(2)} sqm ({tilesPerBox} tiles @ {tileWidth}×{tileHeight} mm)
                    </div>
                  )}
                </div>
              )}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem' }}>
                {attributeFields.filter((field) => field.sellerField !== false && field.packageSizeSource !== 'CALCULATED' && !(isTileTemplate && ['dimensionsMm', 'widthMm', 'heightMm', 'piecesPerBox'].includes(field.id))).map((field) => {
                  const label = localized(field.labelI18n, language, field.label);
                  return (
                  <div key={field.id} className="catalog-field">
                    <label htmlFor={`spec-field-${field.id}`}>
                      {label} {field.priority === 'RECOMMENDED' && <small>(recommended)</small>} {field.required && <span style={{ color: '#dc2626' }}>*</span>}
                      {field.unit && <span style={{ color: '#64748b' }}> ({field.unit})</span>}
                    </label>

                    {field.type === 'select' && field.options ? (
                      <select
                        id={`spec-field-${field.id}`}
                        required={field.required}
                        value={specs[field.id] || ''}
                        onChange={(e) => {
                          setSpecs({ ...specs, [field.id]: e.target.value });
                          if (field.packageSizeSource === 'SPECIFICATION_FIELD') setPackageSize(parseFloat(e.target.value) || 0);
                        }}
                        data-testid={`spec-field-${field.id}`}
                      >
                        <option value="">Select {label}</option>
                        {field.options.map((opt) => (
                          <option key={opt} value={opt}>
                            {opt}
                          </option>
                        ))}
                      </select>
                    ) : field.type === 'number' ? (
                      <input
                        id={`spec-field-${field.id}`}
                        type="number"
                        step="any"
                        required={field.required}
                        placeholder={field.placeholder || `Enter ${label}`}
                        value={specs[field.id] ?? ''}
                        onChange={(e) => setSpecs({ ...specs, [field.id]: parseFloat(e.target.value) || '' })}
                        data-testid={`spec-field-${field.id}`}
                      />
                    ) : (
                      <input
                        id={`spec-field-${field.id}`}
                        type="text"
                        required={field.required}
                        placeholder={field.placeholder || `e.g. ${label}`}
                        value={specs[field.id] ?? ''}
                        onChange={(e) => {
                          setSpecs({ ...specs, [field.id]: e.target.value });
                          if (field.packageSizeSource === 'SPECIFICATION_FIELD') setPackageSize(parseFloat(e.target.value) || 0);
                        }}
                        data-testid={`spec-field-${field.id}`}
                      />
                    )}
                    {field.helper && <small>{field.helper}</small>}
                  </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Custom Item: How is it sold? */}
          {isCustomSelected && (
            <div
              style={{
                padding: '1.2rem',
                background: '#fffaf0',
                border: '1px solid #fed7aa',
                borderRadius: '10px',
                display: 'flex',
                flexDirection: 'column',
                gap: '0.85rem',
              }}
              data-testid="custom-sale-type-selector"
            >
              <label style={{ fontWeight: 650, color: '#7b341e' }}>How is this item sold?</label>
              <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
                <button
                  type="button"
                  onClick={() => setCustomSaleType('PIECE')}
                  style={{
                    flex: 1,
                    padding: '0.65rem 1rem',
                    borderRadius: '8px',
                    border: '1px solid',
                    borderColor: customSaleType === 'PIECE' ? '#ea751b' : '#cbd5e1',
                    background: customSaleType === 'PIECE' ? '#fff7ed' : '#ffffff',
                    fontWeight: customSaleType === 'PIECE' ? 650 : 500,
                    cursor: 'pointer',
                  }}
                  data-testid="custom-type-piece"
                >
                  As Individual Pieces / Units (e.g. 1 drill, 2 generators)
                </button>
                <button
                  type="button"
                  onClick={() => setCustomSaleType('PACKAGE')}
                  style={{
                    flex: 1,
                    padding: '0.65rem 1rem',
                    borderRadius: '8px',
                    border: '1px solid',
                    borderColor: customSaleType === 'PACKAGE' ? '#ea751b' : '#cbd5e1',
                    background: customSaleType === 'PACKAGE' ? '#fff7ed' : '#ffffff',
                    fontWeight: customSaleType === 'PACKAGE' ? 650 : 500,
                    cursor: 'pointer',
                  }}
                  data-testid="custom-type-package"
                >
                  In Packages / Containers (e.g. 10 bags of 25kg)
                </button>
                <button
                  type="button"
                  onClick={() => setCustomSaleType('BULK')}
                  style={{
                    flex: 1,
                    padding: '0.65rem 1rem',
                    borderRadius: '8px',
                    border: '1px solid',
                    borderColor: customSaleType === 'BULK' ? '#ea751b' : '#cbd5e1',
                    background: customSaleType === 'BULK' ? '#fff7ed' : '#ffffff',
                    fontWeight: customSaleType === 'BULK' ? 650 : 500,
                    cursor: 'pointer',
                  }}
                  data-testid="custom-type-bulk"
                >
                  In Bulk / Continuous Quantity (e.g. 50 kg sand)
                </button>
              </div>

              {customSaleType === 'PACKAGE' && (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginTop: '0.5rem' }}>
                  <div className="catalog-field">
                    <label>Package Type</label>
                    <input
                      type="text"
                      placeholder="e.g. box, bag, pack"
                      value={customPackageTypeName}
                      onChange={(e) => setCustomPackageTypeName(e.target.value)}
                    />
                  </div>
                  <div className="catalog-field">
                    <label>Content Unit</label>
                    <input
                      type="text"
                      placeholder="e.g. kg, L, pcs"
                      value={customUnit}
                      onChange={(e) => setCustomUnit(e.target.value)}
                    />
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Friendly Quantity & Pricing Form (ZERO INTERNAL JARGON) */}
          <div
            style={{
              padding: '1.25rem',
              background: '#ffffff',
              border: '1px solid #e2e8f0',
              borderRadius: '10px',
              display: 'flex',
              flexDirection: 'column',
              gap: '1.2rem',
            }}
            data-testid="quantity-pricing-section"
          >
            <div style={{ fontWeight: 650, fontSize: '0.95rem', color: '#1e293b' }}>
              Quantity &amp; Pricing
            </div>

            {effectiveMode === 'PACKAGE' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
                {!packageSource && <div className="catalog-field">
                  <label>
                    Container / Package Size ({selectedTemplate?.baseUnit || customUnit || 'units'}) *
                  </label>
                  {selectedTemplate?.allowedPackageSizes && selectedTemplate.allowedPackageSizes.length > 0 ? (
                    <select
                      value={packageSize}
                      onChange={(e) => setPackageSize(parseFloat(e.target.value) || 1)}
                      data-testid="package-size-select"
                    >
                      {selectedTemplate.allowedPackageSizes.map((s) => (
                        <option key={s} value={s}>
                          {s} {selectedTemplate.baseUnit}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <input
                      type="number"
                      step="any"
                      min="0.01"
                      required
                      value={packageSize || ''}
                      onChange={(e) => setPackageSize(parseFloat(e.target.value) || 0)}
                      placeholder="e.g. 4"
                      data-testid="package-size-input"
                    />
                  )}
                </div>}

                <div className="catalog-field">
                  <label>
                    Number of {selectedTemplate?.packageType ? `${selectedTemplate.packageType}s` : 'packages'} *
                  </label>
                  <input
                    type="number"
                    min="1"
                    step="1"
                    required
                    value={packageCount || ''}
                    onChange={(e) => setPackageCount(parseInt(e.target.value, 10) || 0)}
                    placeholder="e.g. 5"
                    data-testid="package-count-input"
                    ref={element => { fieldRefs.current.packageCount = element; }}
                    aria-invalid={Boolean(fieldErrors.packageCount)}
                  />
                  {fieldErrors.packageCount && <small className="field-error">{fieldErrors.packageCount}</small>}
                </div>

                <div className="catalog-field">
                  <label>
                    Price per {selectedTemplate?.packageType || 'package'} (LKR) *
                  </label>
                  <input
                    type="number"
                    min="1"
                    step="any"
                    required
                    value={pricePerPackage || ''}
                    onChange={(e) => setPricePerPackage(parseFloat(e.target.value) || 0)}
                    placeholder="e.g. 4500"
                    data-testid="price-per-package-input"
                    ref={element => { fieldRefs.current.pricePerPackage = element; }}
                    aria-invalid={Boolean(fieldErrors.pricePerPackage)}
                  />
                  {fieldErrors.pricePerPackage && <small className="field-error">{fieldErrors.pricePerPackage}</small>}
                </div>
              </div>
            )}

            {effectiveMode === 'PIECE' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
                <div className="catalog-field">
                  <label>Number of items available *</label>
                  <input
                    type="number"
                    min="1"
                    step="1"
                    required
                    value={pieceCount || ''}
                    onChange={(e) => setPieceCount(parseInt(e.target.value, 10) || 0)}
                    placeholder="e.g. 1"
                    data-testid="piece-count-input"
                    ref={element => { fieldRefs.current.quantity = element; }}
                    aria-invalid={Boolean(fieldErrors.quantity)}
                  />
                  {fieldErrors.quantity && <small className="field-error">{fieldErrors.quantity}</small>}
                </div>

                <div className="catalog-field">
                  <label>Price per item / unit (LKR) *</label>
                  <input
                    type="number"
                    min="1"
                    step="any"
                    required
                    value={pricePerPiece || ''}
                    onChange={(e) => setPricePerPiece(parseFloat(e.target.value) || 0)}
                    placeholder="e.g. 150000"
                    data-testid="price-per-piece-input"
                    ref={element => { fieldRefs.current.unitPrice = element; }}
                    aria-invalid={Boolean(fieldErrors.unitPrice)}
                  />
                  {fieldErrors.unitPrice && <small className="field-error">{fieldErrors.unitPrice}</small>}
                </div>
              </div>
            )}

            {effectiveMode === 'CONTINUOUS' && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
                <div className="catalog-field">
                  <label>Total quantity *</label>
                  <input
                    type="number"
                    min="0.1"
                    step="any"
                    required
                    value={bulkQuantity || ''}
                    onChange={(e) => setBulkQuantity(parseFloat(e.target.value) || 0)}
                    placeholder="e.g. 5"
                    data-testid="bulk-quantity-input"
                    ref={element => { fieldRefs.current.quantity = element; }}
                    aria-invalid={Boolean(fieldErrors.quantity)}
                  />
                  {fieldErrors.quantity && <small className="field-error">{fieldErrors.quantity}</small>}
                </div>

                <div className="catalog-field">
                  <label>Unit *</label>
                  {selectedTemplate?.allowedUnits && selectedTemplate.allowedUnits.length > 0 ? (
                    <select
                      value={selectedUnit}
                      onChange={(e) => setSelectedUnit(e.target.value)}
                      data-testid="bulk-unit-select"
                    >
                      {selectedTemplate.allowedUnits.map((u) => (
                        <option key={u} value={u}>
                          {u}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <input
                      type="text"
                      required
                      value={selectedUnit}
                      onChange={(e) => setSelectedUnit(e.target.value)}
                      placeholder="e.g. Cubes, Tons, kg"
                      data-testid="bulk-unit-input"
                    />
                  )}
                </div>

                <div className="catalog-field">
                  <label>Price per {selectedUnit || 'unit'} (LKR) *</label>
                  <input
                    type="number"
                    min="1"
                    step="any"
                    required
                    value={pricePerBulkUnit || ''}
                    onChange={(e) => setPricePerBulkUnit(parseFloat(e.target.value) || 0)}
                    placeholder="e.g. 12000"
                    data-testid="price-per-bulk-input"
                    ref={element => { fieldRefs.current.unitPrice = element; }}
                    aria-invalid={Boolean(fieldErrors.unitPrice)}
                  />
                  {fieldErrors.unitPrice && <small className="field-error">{fieldErrors.unitPrice}</small>}
                </div>
              </div>
            )}

            {/* Clear summary display for seller confidence */}
            <div
              data-testid="quantity-summary-box"
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '0.75rem 1rem',
                background: '#f8fafc',
                borderRadius: '8px',
                border: '1px solid #e2e8f0',
                fontSize: '0.88rem',
              }}
            >
              <div>
                Total Sellable Stock:{' '}
                <strong>
                  {calculatedTotalQuantity}{' '}
                  {effectiveMode === 'PACKAGE'
                    ? selectedTemplate?.baseUnit || customUnit || 'units'
                    : effectiveMode === 'PIECE'
                    ? 'items'
                    : selectedUnit}
                </strong>
                {effectiveMode === 'PACKAGE' && (
                  <span style={{ color: '#64748b' }}>
                    {' '}
                    ({packageCount} {selectedTemplate?.packageType || 'package'}s &times; {packageSize}{' '}
                    {selectedTemplate?.baseUnit || customUnit})
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* Description & Location */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1rem' }}>
            <div className="catalog-field" style={{ gridColumn: '1 / -1' }}>
              <label>Description / Additional Notes</label>
              <textarea
                rows={3}
                placeholder="Provide helpful details for buyers (brand, storage conditions, exact model, accessories included)..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                data-testid="textarea-listing-description"
              />
            </div>

            <div className="catalog-field">
              <label>Pickup Location / City *</label>
              <input
                type="text"
                required
                placeholder="e.g. Colombo 03, Kandy, Gampaha"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                data-testid="input-listing-location"
              />
            </div>
          </div>

          {/* Form Actions */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
            {onCancel && (
              <button type="button" className="button" onClick={onCancel}>
                Cancel
              </button>
            )}
            <button
              type="submit"
              className="button button-primary"
              disabled={isSubmitting}
              data-testid="submit-listing-btn"
            >
              {isSubmitting ? 'Submitting Listing...' : 'Publish Listing'}
            </button>
          </div>
        </div>
      )}
    </form>
  );
}

function normalizeField(field: string): string {
  const compact = field.replace(/^\$?\.?/u, '').replaceAll('.', '').toLowerCase();
  if (compact.includes('packagecount')) return 'packageCount';
  if (compact.includes('packagesize')) return 'packageSize';
  if (compact.includes('price')) return compact.includes('package') ? 'pricePerPackage' : 'unitPrice';
  if (compact.includes('quantity')) return 'quantity';
  if (compact.includes('category')) return 'categoryId';
  if (compact.includes('title')) return 'title';
  return field;
}

function localizedValidation(message: string | undefined, field: string, t: (key: string) => string): string {
  const code = (message ?? '').toUpperCase();
  if (code.includes('PACKAGE_COUNT')) return t('packageCountRequired');
  if (code.includes('PRICE') || field.toLowerCase().includes('price')) return t('priceRequired');
  if (code.includes('QUANTITY') || field.toLowerCase().includes('quantity')) return t('quantityRequired');
  return t('required');
}

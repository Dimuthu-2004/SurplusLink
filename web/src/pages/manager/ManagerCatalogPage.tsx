import React, { FormEvent, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  fetchItemTemplates,
  createItemTemplate,
  updateItemTemplate,
  toggleItemTemplateStatus,
  type ConstructionItemTemplate,
  type CreateConstructionItemTemplatePayload,
  type AttributeFieldDefinition,
} from '../../api/constructionItemTemplatesApi';
import {
  managerMaterialsApi,
  type MaterialCategory,
} from '../../features/materials/managerMaterialsApi';
import './managerCatalog.css';

interface VisualFieldItem {
  id: string;
  label: string;
  type: 'string' | 'number' | 'select';
  required: boolean;
  unit?: string;
  options: string[];
  placeholder?: string;
}

interface TemplateFormState {
  id?: string;
  name: string;
  categoryId: string;
  itemClass: string;
  quantityMode: string;
  baseUnit: string;
  packageType: string;
  allowedUnits: string;
  allowedPackageSizes: string;
  priceBasis: string;
  attributeSchema: string;
}

const defaultFormState: TemplateFormState = {
  name: '',
  categoryId: '',
  itemClass: 'MATERIAL',
  quantityMode: 'PIECE',
  baseUnit: 'unit',
  packageType: '',
  allowedUnits: 'unit',
  allowedPackageSizes: '',
  priceBasis: 'PER_UNIT',
  attributeSchema: '[]',
};

function schemaToVisualFields(schemaJson: string): VisualFieldItem[] {
  try {
    const parsed = JSON.parse(schemaJson);
    if (!Array.isArray(parsed)) return [];
    return parsed.map((item: any, idx: number) => ({
      id: item.id || `field_${idx + 1}`,
      label: item.label || '',
      type: item.type === 'number' ? 'number' : item.type === 'select' ? 'select' : 'string',
      required: Boolean(item.required),
      unit: item.unit || '',
      options: Array.isArray(item.options) ? item.options : [],
      placeholder: item.placeholder || '',
    }));
  } catch {
    return [];
  }
}

function visualFieldsToSchema(fields: VisualFieldItem[]): string {
  const schema = fields.map((f) => {
    const item: Record<string, any> = {
      id: f.id.trim() || f.label.toLowerCase().replace(/[^a-z0-9]/g, '_'),
      label: f.label.trim(),
      type: f.type,
      required: f.required,
    };
    if (f.placeholder?.trim()) item.placeholder = f.placeholder.trim();
    if (f.unit?.trim()) item.unit = f.unit.trim();
    if (f.type === 'select' && f.options.length > 0) {
      item.options = f.options;
    }
    return item;
  });
  return JSON.stringify(schema, null, 2);
}

export function ManagerCatalogPage() {
  const [templates, setTemplates] = useState<ConstructionItemTemplate[]>([]);
  const [categories, setCategories] = useState<MaterialCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedClass, setSelectedClass] = useState('');
  const [includeInactive, setIncludeInactive] = useState(true);

  // Modal states
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [formState, setFormState] = useState<TemplateFormState>(defaultFormState);
  const [visualFields, setVisualFields] = useState<VisualFieldItem[]>([]);
  const [newChoiceInputs, setNewChoiceInputs] = useState<Record<number, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Schema viewer modal
  const [schemaModalTemplate, setSchemaModalTemplate] = useState<ConstructionItemTemplate | null>(null);

  useEffect(() => {
    let active = true;
    void managerMaterialsApi.getCategories().then(
      (items) => active && setCategories(items),
      () => active && setCategories([]),
    );
    return () => {
      active = false;
    };
  }, []);

  const loadTemplates = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchItemTemplates({
        search: search.trim() || undefined,
        categoryId: selectedCategory || undefined,
        itemClass: selectedClass || undefined,
        includeInactive,
      });
      setTemplates(data);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load item templates.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadTemplates();
  }, [search, selectedCategory, selectedClass, includeInactive]);

  const openCreateModal = () => {
    setFormState({
      ...defaultFormState,
      categoryId: categories.length > 0 ? categories[0].id : '',
    });
    setVisualFields([]);
    setFormError(null);
    setIsEditModalOpen(true);
  };

  const openEditModal = (t: ConstructionItemTemplate) => {
    setFormState({
      id: t.id,
      name: t.name,
      categoryId: t.categoryId,
      itemClass: t.itemClass,
      quantityMode: t.quantityMode,
      baseUnit: t.baseUnit,
      packageType: t.packageType || '',
      allowedUnits: t.allowedUnits.join(', '),
      allowedPackageSizes: t.allowedPackageSizes.join(', '),
      priceBasis: t.priceBasis || (t.quantityMode === 'PACKAGE' ? 'PER_PACKAGE' : 'PER_UNIT'),
      attributeSchema: t.attributeSchema || '[]',
    });
    setVisualFields(schemaToVisualFields(t.attributeSchema || '[]'));
    setFormError(null);
    setIsEditModalOpen(true);
  };

  const handleToggleStatus = async (template: ConstructionItemTemplate) => {
    try {
      const updated = await toggleItemTemplateStatus(template.id, !template.isActive);
      setTemplates((prev) =>
        prev.map((item) => (item.id === updated.id ? updated : item)),
      );
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : 'Could not toggle status.');
    }
  };

  const syncVisualFieldsToSchema = (fields: VisualFieldItem[]) => {
    setVisualFields(fields);
    setFormState((prev) => ({
      ...prev,
      attributeSchema: visualFieldsToSchema(fields),
    }));
  };

  const addField = () => {
    const newField: VisualFieldItem = {
      id: `field_${Date.now()}`,
      label: '',
      type: 'string',
      required: false,
      options: [],
    };
    syncVisualFieldsToSchema([...visualFields, newField]);
  };

  const updateField = (index: number, updates: Partial<VisualFieldItem>) => {
    const updated = [...visualFields];
    updated[index] = { ...updated[index], ...updates };
    if (updates.label && !updates.id && updated[index].id.startsWith('field_')) {
      updated[index].id = updates.label.toLowerCase().replace(/[^a-z0-9]/g, '_');
    }
    syncVisualFieldsToSchema(updated);
  };

  const removeField = (index: number) => {
    const updated = visualFields.filter((_, i) => i !== index);
    syncVisualFieldsToSchema(updated);
  };

  const moveField = (index: number, direction: 'up' | 'down') => {
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= visualFields.length) return;
    const updated = [...visualFields];
    const temp = updated[index];
    updated[index] = updated[targetIndex];
    updated[targetIndex] = temp;
    syncVisualFieldsToSchema(updated);
  };

  const addChoice = (fieldIndex: number) => {
    const choiceText = (newChoiceInputs[fieldIndex] || '').trim();
    if (!choiceText) return;
    const currentOptions = visualFields[fieldIndex].options || [];
    if (!currentOptions.includes(choiceText)) {
      updateField(fieldIndex, { options: [...currentOptions, choiceText] });
    }
    setNewChoiceInputs({ ...newChoiceInputs, [fieldIndex]: '' });
  };

  const removeChoice = (fieldIndex: number, choiceIndex: number) => {
    const currentOptions = visualFields[fieldIndex].options || [];
    updateField(fieldIndex, {
      options: currentOptions.filter((_, i) => i !== choiceIndex),
    });
  };

  const handleSaveTemplate = async (e: FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!formState.name.trim()) {
      setFormError('Item name is required.');
      return;
    }
    if (!formState.categoryId) {
      setFormError('Category is required.');
      return;
    }
    if (!formState.baseUnit.trim()) {
      setFormError('Base unit is required.');
      return;
    }

    // Serialize visual fields to schema
    const schemaJson = visualFields.length > 0
      ? visualFieldsToSchema(visualFields)
      : formState.attributeSchema || '[]';

    let parsedSchema: unknown;
    try {
      parsedSchema = JSON.parse(schemaJson);
      if (!Array.isArray(parsedSchema)) {
        setFormError('Attribute schema must be a JSON array of field definitions.');
        return;
      }
    } catch {
      setFormError('Invalid JSON format for attribute schema.');
      return;
    }

    const allowedUnits = formState.allowedUnits
      .split(',')
      .map((u) => u.trim())
      .filter(Boolean);
    if (allowedUnits.length === 0) {
      allowedUnits.push(formState.baseUnit.trim());
    }

    const allowedPackageSizes = formState.allowedPackageSizes
      .split(',')
      .map((s) => parseFloat(s.trim()))
      .filter((n) => !isNaN(n) && n > 0);

    const payload: CreateConstructionItemTemplatePayload = {
      name: formState.name.trim(),
      categoryId: formState.categoryId,
      itemClass: formState.itemClass,
      quantityMode: formState.quantityMode,
      baseUnit: formState.baseUnit.trim(),
      packageType: formState.packageType.trim() || null,
      allowedUnits,
      allowedPackageSizes,
      attributeSchema: JSON.stringify(parsedSchema),
      priceBasis: formState.priceBasis || undefined,
    };

    setSaving(true);
    try {
      if (formState.id) {
        const updated = await updateItemTemplate(formState.id, payload);
        setTemplates((prev) =>
          prev.map((item) => (item.id === updated.id ? updated : item)),
        );
      } else {
        const created = await createItemTemplate(payload);
        setTemplates((prev) => [created, ...prev]);
      }
      setIsEditModalOpen(false);
    } catch (err: unknown) {
      setFormError(err instanceof Error ? err.message : 'Failed to save catalog item.');
    } finally {
      setSaving(false);
    }
  };

  const parsedModalFields = useMemo<AttributeFieldDefinition[]>(() => {
    if (!schemaModalTemplate?.attributeSchema) return [];
    try {
      const parsed = JSON.parse(schemaModalTemplate.attributeSchema);
      return Array.isArray(parsed) ? (parsed as AttributeFieldDefinition[]) : [];
    } catch {
      return [];
    }
  }, [schemaModalTemplate]);

  return (
    <div className="manager-page catalog-container">
      <Link className="back-link" to="/app/manager">
        Back to Dashboard
      </Link>

      <section className="page-heading">
        <p className="eyebrow">Inventory Management</p>
        <h1>Item Catalog</h1>
        <p className="muted">
          Configure catalog templates for construction materials, tools, and equipment.
          Sellers use these items to list stock with clear unit packaging and attributes.
        </p>
      </section>

      <div className="catalog-header-actions">
        <button
          type="button"
          className="button button-primary"
          onClick={openCreateModal}
          data-testid="create-template-btn"
        >
          + Add Catalog Item
        </button>
      </div>

      <div className="catalog-filters">
        <input
          type="text"
          className="catalog-search-input"
          placeholder="Search by item name or category..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          data-testid="catalog-search"
        />

        <select
          className="catalog-select"
          value={selectedCategory}
          onChange={(e) => setSelectedCategory(e.target.value)}
          data-testid="catalog-category-filter"
        >
          <option value="">All Categories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>

        <select
          className="catalog-select"
          value={selectedClass}
          onChange={(e) => setSelectedClass(e.target.value)}
          data-testid="catalog-class-filter"
        >
          <option value="">All Item Types</option>
          <option value="MATERIAL">Material</option>
          <option value="TOOL">Tool</option>
          <option value="EQUIPMENT">Equipment</option>
          <option value="FIXTURE">Fixture</option>
          <option value="TEMPORARY_WORK">Temporary Work</option>
          <option value="OTHER_CONSTRUCTION">Other</option>
        </select>

        <label className="catalog-checkbox-label">
          <input
            type="checkbox"
            checked={includeInactive}
            onChange={(e) => setIncludeInactive(e.target.checked)}
          />
          Show Inactive Items
        </label>
      </div>

      {error && <div className="error-banner">{error}</div>}

      <div className="catalog-table-wrapper">
        {loading ? (
          <div className="analytics-loading">Loading item catalog...</div>
        ) : templates.length === 0 ? (
          <div className="empty-state">No catalog items found matching your criteria.</div>
        ) : (
          <table className="catalog-table" data-testid="catalog-table">
            <thead>
              <tr>
                <th>Item Template</th>
                <th>Category</th>
                <th>Item Type</th>
                <th>Selling Format</th>
                <th>Base Unit</th>
                <th>Packaging</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {templates.map((t) => (
                <tr key={t.id} data-testid={`template-row-${t.id}`}>
                  <td>
                    <strong>{t.name}</strong>
                  </td>
                  <td>{t.categoryName}</td>
                  <td>
                    <span className={`badge-item-class badge-class-${t.itemClass.toLowerCase()}`}>
                      {t.itemClass}
                    </span>
                  </td>
                  <td>
                    {t.quantityMode === 'PACKAGE'
                      ? 'Package / Container'
                      : t.quantityMode === 'PIECE'
                      ? 'Piece / Unit'
                      : 'Measured Quantity'}
                  </td>
                  <td>{t.baseUnit}</td>
                  <td>
                    {t.packageType
                      ? `${t.packageType} (${t.allowedPackageSizes?.length ? t.allowedPackageSizes.join(', ') : 'any'} ${t.baseUnit})`
                      : '—'}
                  </td>
                  <td>
                    <span
                      className={`status-badge ${t.isActive ? 'badge-success' : 'badge-inactive'}`}
                      style={{
                        padding: '0.2rem 0.5rem',
                        borderRadius: '4px',
                        background: t.isActive ? '#dcfce7' : '#f1f5f9',
                        color: t.isActive ? '#15803d' : '#64748b',
                      }}
                    >
                      {t.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <div className="catalog-actions-cell">
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => setSchemaModalTemplate(t)}
                        title="View Form Fields"
                        data-testid={`view-schema-${t.id}`}
                      >
                        Form Fields
                      </button>
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => openEditModal(t)}
                        title="Edit Item"
                        data-testid={`edit-template-${t.id}`}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className={`btn-icon ${t.isActive ? 'btn-toggle-inactive' : 'btn-toggle-active'}`}
                        onClick={() => handleToggleStatus(t)}
                        data-testid={`toggle-template-${t.id}`}
                      >
                        {t.isActive ? 'Disable' : 'Enable'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Add / Edit Template Modal */}
      {isEditModalOpen && (
        <div className="catalog-modal-overlay" role="dialog" aria-modal="true">
          <div className="catalog-modal">
            <div className="catalog-modal-header">
              <h2>{formState.id ? 'Edit Catalog Item' : 'Add Catalog Item'}</h2>
              <button
                type="button"
                className="catalog-modal-close"
                onClick={() => setIsEditModalOpen(false)}
                aria-label="Close"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18"></line>
                  <line x1="6" y1="6" x2="18" y2="18"></line>
                </svg>
              </button>
            </div>
            <form onSubmit={handleSaveTemplate}>
              <div className="catalog-modal-body">
                {formError && <div className="error-banner">{formError}</div>}

                <div className="form-grid-2">
                  <div className="catalog-field">
                    <label>Item Name *</label>
                    <input
                      type="text"
                      required
                      placeholder="e.g. Paint, Cement, Generator, Tiles"
                      value={formState.name}
                      onChange={(e) => setFormState({ ...formState, name: e.target.value })}
                      data-testid="input-template-name"
                    />
                  </div>

                  <div className="catalog-field">
                    <label>Category *</label>
                    <select
                      required
                      value={formState.categoryId}
                      onChange={(e) => setFormState({ ...formState, categoryId: e.target.value })}
                      data-testid="select-template-category"
                    >
                      <option value="">Select Category</option>
                      {categories.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="form-grid-2">
                  <div className="catalog-field">
                    <label>Item Type *</label>
                    <select
                      value={formState.itemClass}
                      onChange={(e) => setFormState({ ...formState, itemClass: e.target.value })}
                    >
                      <option value="MATERIAL">Material</option>
                      <option value="TOOL">Tool</option>
                      <option value="EQUIPMENT">Equipment</option>
                      <option value="FIXTURE">Fixture</option>
                      <option value="TEMPORARY_WORK">Temporary Work</option>
                      <option value="OTHER_CONSTRUCTION">Other Construction</option>
                    </select>
                  </div>

                  <div className="catalog-field">
                    <label>Selling Format *</label>
                    <select
                      value={formState.quantityMode}
                      onChange={(e) => setFormState({ ...formState, quantityMode: e.target.value })}
                    >
                      <option value="PIECE">Sold by unit or piece (e.g. tools, fixtures, items)</option>
                      <option value="PACKAGE">Sold in packages or containers (e.g. cans, bags, boxes)</option>
                      <option value="CONTINUOUS_BULK">Sold by measured quantity / bulk (e.g. sand, gravel, bulk liquid)</option>
                    </select>
                  </div>
                </div>

                <div className="form-grid-2">
                  <div className="catalog-field">
                    <label>Base Unit *</label>
                    <input
                      type="text"
                      required
                      placeholder="e.g. piece, L, kg, sqm, m3"
                      value={formState.baseUnit}
                      onChange={(e) => setFormState({ ...formState, baseUnit: e.target.value })}
                    />
                  </div>

                  {formState.quantityMode === 'PACKAGE' ? (
                    <div className="catalog-field">
                      <label>Package Container Type (e.g. can, bag, box)</label>
                      <input
                        type="text"
                        placeholder="e.g. can, bag, box, cartridge, drum"
                        value={formState.packageType}
                        onChange={(e) => setFormState({ ...formState, packageType: e.target.value })}
                      />
                    </div>
                  ) : (
                    <div className="catalog-field">
                      <label>Allowed Units (comma-separated)</label>
                      <input
                        type="text"
                        placeholder="e.g. piece, unit"
                        value={formState.allowedUnits}
                        onChange={(e) => setFormState({ ...formState, allowedUnits: e.target.value })}
                      />
                    </div>
                  )}
                </div>

                {formState.quantityMode === 'PACKAGE' && (
                  <div className="form-grid-2">
                    <div className="catalog-field">
                      <label>Allowed Package Sizes (comma-separated)</label>
                      <input
                        type="text"
                        placeholder="e.g. 1, 4, 10, 20"
                        value={formState.allowedPackageSizes}
                        onChange={(e) =>
                          setFormState({ ...formState, allowedPackageSizes: e.target.value })
                        }
                      />
                    </div>
                    <div className="catalog-field">
                      <label>Allowed Units (comma-separated)</label>
                      <input
                        type="text"
                        placeholder="e.g. L, can"
                        value={formState.allowedUnits}
                        onChange={(e) => setFormState({ ...formState, allowedUnits: e.target.value })}
                      />
                    </div>
                  </div>
                )}

                {/* Visual Field Builder for Seller Details */}
                <div className="field-builder-section">
                  <div className="field-builder-header">
                    <div>
                      <span className="field-builder-title">Seller details to request</span>
                      <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: '#64748b' }}>
                        Define specific attributes sellers must fill when listing this item (e.g. thickness, fuel type, colour).
                      </p>
                    </div>
                    <button
                      type="button"
                      className="button"
                      style={{ fontSize: '0.82rem', padding: '0.4rem 0.75rem' }}
                      onClick={addField}
                    >
                      + Add field
                    </button>
                  </div>

                  {visualFields.length === 0 ? (
                    <div style={{ textAlign: 'center', padding: '1rem', color: '#94a3b8', fontSize: '0.85rem' }}>
                      No custom fields added yet. Only standard quantity and condition will be requested.
                    </div>
                  ) : (
                    visualFields.map((field, idx) => (
                      <div key={field.id} className="field-card">
                        <div className="field-card-top">
                          <input
                            type="text"
                            placeholder="Field Label (e.g. Thickness, Colour)"
                            value={field.label}
                            onChange={(e) => updateField(idx, { label: e.target.value })}
                            style={{ flex: 2 }}
                          />
                          <select
                            value={field.type}
                            onChange={(e) => updateField(idx, { type: e.target.value as any })}
                            style={{ flex: 1.2 }}
                          >
                            <option value="string">Text</option>
                            <option value="number">Number</option>
                            <option value="select">Choice / Options</option>
                          </select>
                          <input
                            type="text"
                            placeholder="Unit / suffix (e.g. mm, L)"
                            value={field.unit || ''}
                            onChange={(e) => updateField(idx, { unit: e.target.value })}
                            style={{ flex: 1 }}
                          />
                          <label style={{ display: 'inline-flex', alignItems: 'center', gap: '0.3rem', fontSize: '0.8rem', userSelect: 'none', cursor: 'pointer' }}>
                            <input
                              type="checkbox"
                              checked={field.required}
                              onChange={(e) => updateField(idx, { required: e.target.checked })}
                            />
                            Required
                          </label>
                          <div className="field-actions">
                            <button
                              type="button"
                              className="btn-field-action"
                              title="Move up"
                              disabled={idx === 0}
                              onClick={() => moveField(idx, 'up')}
                            >
                              ↑
                            </button>
                            <button
                              type="button"
                              className="btn-field-action"
                              title="Move down"
                              disabled={idx === visualFields.length - 1}
                              onClick={() => moveField(idx, 'down')}
                            >
                              ↓
                            </button>
                            <button
                              type="button"
                              className="btn-field-action btn-field-remove"
                              title="Remove field"
                              onClick={() => removeField(idx)}
                            >
                              ✕
                            </button>
                          </div>
                        </div>

                        {field.type === 'select' && (
                          <div className="field-card-options">
                            <span style={{ fontSize: '0.78rem', fontWeight: 600, color: '#475569' }}>Choices:</span>
                            {field.options.map((opt, optIdx) => (
                              <span key={optIdx} className="choice-chip">
                                {opt}
                                <button type="button" onClick={() => removeChoice(idx, optIdx)}>×</button>
                              </span>
                            ))}
                            <div className="choice-add-input">
                              <input
                                type="text"
                                placeholder="Add choice..."
                                value={newChoiceInputs[idx] || ''}
                                onChange={(e) => setNewChoiceInputs({ ...newChoiceInputs, [idx]: e.target.value })}
                                onKeyDown={(e) => {
                                  if (e.key === 'Enter') {
                                    e.preventDefault();
                                    addChoice(idx);
                                  }
                                }}
                              />
                              <button
                                type="button"
                                className="button"
                                style={{ padding: '0.2rem 0.5rem', fontSize: '0.75rem' }}
                                onClick={() => addChoice(idx)}
                              >
                                + Add
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    ))
                  )}

                  {/* Collapsed Advanced Schema Details for Developers */}
                  <details className="advanced-schema-details">
                    <summary>Advanced details (Developer JSON)</summary>
                    <textarea
                      rows={4}
                      style={{ width: '100%', marginTop: '0.5rem', fontFamily: 'monospace', fontSize: '0.8rem' }}
                      value={formState.attributeSchema}
                      onChange={(e) => {
                        setFormState({ ...formState, attributeSchema: e.target.value });
                        setVisualFields(schemaToVisualFields(e.target.value));
                      }}
                    />
                  </details>
                </div>
              </div>

              <div className="catalog-modal-footer">
                <button
                  type="button"
                  className="button"
                  onClick={() => setIsEditModalOpen(false)}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="button button-primary"
                  disabled={saving}
                  data-testid="save-template-submit"
                >
                  {saving ? 'Saving...' : formState.id ? 'Save Changes' : 'Create Catalog Item'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Schema / Form Fields Viewer Modal */}
      {schemaModalTemplate && (
        <div className="catalog-modal-overlay" role="dialog" aria-modal="true">
          <div className="catalog-modal">
            <div className="catalog-modal-header">
              <h2>Seller Form Fields: {schemaModalTemplate.name}</h2>
              <button
                type="button"
                className="catalog-modal-close"
                onClick={() => setSchemaModalTemplate(null)}
                aria-label="Close"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18"></line>
                  <line x1="6" y1="6" x2="18" y2="18"></line>
                </svg>
              </button>
            </div>
            <div className="catalog-modal-body">
              <p style={{ margin: 0, color: '#475569', fontSize: '0.9rem' }}>
                These specific fields will be presented to sellers when &quot;{schemaModalTemplate.name}&quot; is selected:
              </p>

              {parsedModalFields.length === 0 ? (
                <div className="empty-state">No custom form fields configured for this item. Standard quantity and condition will be requested.</div>
              ) : (
                <div className="schema-field-preview">
                  {parsedModalFields.map((f) => (
                    <div key={f.id} className="schema-field-row">
                      <div>
                        <strong>{f.label}</strong> {f.required && <span style={{ color: '#dc2626' }}>* (Required)</span>}
                        <div style={{ color: '#64748b', fontSize: '0.78rem' }}>
                          Type: <code>{f.type}</code>
                          {f.unit && ` • Unit: ${f.unit}`}
                        </div>
                      </div>
                      <div>
                        {f.options && f.options.length > 0 && (
                          <span style={{ fontSize: '0.8rem', color: '#0369a1' }}>
                            Choices: {f.options.join(', ')}
                          </span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <details className="advanced-schema-details" style={{ marginTop: '0.75rem' }}>
                <summary>Advanced details (Developer JSON)</summary>
                <pre
                  style={{
                    background: '#f1f5f9',
                    padding: '0.75rem',
                    borderRadius: '8px',
                    fontSize: '0.78rem',
                    maxHeight: '180px',
                    overflowY: 'auto',
                    marginTop: '0.5rem',
                  }}
                >
                  {schemaModalTemplate.attributeSchema}
                </pre>
              </details>
            </div>
            <div className="catalog-modal-footer">
              <button
                type="button"
                className="button button-primary"
                onClick={() => setSchemaModalTemplate(null)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

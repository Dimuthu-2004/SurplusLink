import { FormEvent, useEffect, useMemo, useState } from 'react';
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

  const handleSaveTemplate = async (e: FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!formState.name.trim()) {
      setFormError('Template name is required.');
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

    // Validate JSON schema
    let parsedSchema: unknown;
    try {
      parsedSchema = JSON.parse(formState.attributeSchema || '[]');
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
      setFormError(err instanceof Error ? err.message : 'Failed to save template.');
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
        <h1>Construction Item Catalog</h1>
        <p className="muted">
          Configure construction templates for materials, tools, and equipment.
          Sellers use these templates to list items without dealing with internal quantity models.
        </p>
      </section>

      <div className="catalog-header-actions">
        <button
          type="button"
          className="button button-primary"
          onClick={openCreateModal}
          data-testid="create-template-btn"
        >
          + Add New Item Template
        </button>
      </div>

      <div className="catalog-filters">
        <input
          type="text"
          className="catalog-search-input"
          placeholder="Search by template or category name..."
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
          <option value="">All Item Classes</option>
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
          <div className="empty-state">No construction item templates found matching your criteria.</div>
        ) : (
          <table className="catalog-table" data-testid="catalog-table">
            <thead>
              <tr>
                <th>Item Template</th>
                <th>Category</th>
                <th>Class</th>
                <th>Quantity Mode</th>
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
                  <td>{t.quantityMode}</td>
                  <td>{t.baseUnit}</td>
                  <td>
                    {t.packageType
                      ? `${t.packageType} (${t.allowedPackageSizes?.length ? t.allowedPackageSizes.join(', ') : 'any'} ${t.baseUnit})`
                      : 'â€”'}
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
                        title="View Attributes Schema"
                      >
                        Schema
                      </button>
                      <button
                        type="button"
                        className="btn-icon"
                        onClick={() => openEditModal(t)}
                        title="Edit Template"
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
              <h2>{formState.id ? 'Edit Construction Item Template' : 'Add Construction Item Template'}</h2>
              <button
                type="button"
                className="catalog-modal-close"
                onClick={() => setIsEditModalOpen(false)}
              >
                Ã—
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
                      placeholder="e.g. Paint, Cement, Generator"
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
                    <label>Item Class *</label>
                    <select
                      value={formState.itemClass}
                      onChange={(e) => setFormState({ ...formState, itemClass: e.target.value })}
                    >
                      <option value="MATERIAL">MATERIAL</option>
                      <option value="TOOL">TOOL</option>
                      <option value="EQUIPMENT">EQUIPMENT</option>
                      <option value="FIXTURE">FIXTURE</option>
                      <option value="TEMPORARY_WORK">TEMPORARY_WORK</option>
                      <option value="OTHER_CONSTRUCTION">OTHER_CONSTRUCTION</option>
                    </select>
                  </div>

                  <div className="catalog-field">
                    <label>Internal Quantity Mode *</label>
                    <select
                      value={formState.quantityMode}
                      onChange={(e) => setFormState({ ...formState, quantityMode: e.target.value })}
                    >
                      <option value="PIECE">PIECE (Unit / Item Count)</option>
                      <option value="PACKAGE">PACKAGE (Can, Bag, Box, etc.)</option>
                      <option value="CONTINUOUS_BULK">CONTINUOUS_BULK (Sand, Gravel, Bulk liquids)</option>
                      <option value="LENGTH">LENGTH (Cables, Pipes, Timber)</option>
                      <option value="AREA">AREA (Tiles, Plywood, Roofing)</option>
                      <option value="VOLUME">VOLUME (Concrete, Bulk liquid)</option>
                    </select>
                  </div>
                </div>

                <div className="form-grid-2">
                  <div className="catalog-field">
                    <label>Base Unit *</label>
                    <input
                      type="text"
                      required
                      placeholder="e.g. L, kg, unit, m, m2"
                      value={formState.baseUnit}
                      onChange={(e) => setFormState({ ...formState, baseUnit: e.target.value })}
                    />
                  </div>

                  <div className="catalog-field">
                    <label>Package Type (if applicable)</label>
                    <input
                      type="text"
                      placeholder="e.g. can, bag, tin, box, drum, roll"
                      value={formState.packageType}
                      onChange={(e) => setFormState({ ...formState, packageType: e.target.value })}
                    />
                  </div>
                </div>

                <div className="form-grid-2">
                  <div className="catalog-field">
                    <label>Allowed Units (comma-separated)</label>
                    <input
                      type="text"
                      placeholder="e.g. L, ml or unit"
                      value={formState.allowedUnits}
                      onChange={(e) => setFormState({ ...formState, allowedUnits: e.target.value })}
                    />
                  </div>

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
                </div>

                <div className="catalog-field">
                  <label>Attributes Schema (JSON Array)</label>
                  <textarea
                    rows={6}
                    value={formState.attributeSchema}
                    onChange={(e) => setFormState({ ...formState, attributeSchema: e.target.value })}
                    placeholder='[{"id":"color","label":"Colour","type":"string","required":true}]'
                  />
                  <small style={{ color: '#64748b' }}>
                    Define dynamic fields shown to sellers (e.g. colour, finish, capacity, fuel, running hours).
                  </small>
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
                  {saving ? 'Saving...' : formState.id ? 'Update Template' : 'Create Template'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Schema Viewer Modal */}
      {schemaModalTemplate && (
        <div className="catalog-modal-overlay" role="dialog" aria-modal="true">
          <div className="catalog-modal">
            <div className="catalog-modal-header">
              <h2>Dynamic Attributes: {schemaModalTemplate.name}</h2>
              <button
                type="button"
                className="catalog-modal-close"
                onClick={() => setSchemaModalTemplate(null)}
              >
                Ã—
              </button>
            </div>
            <div className="catalog-modal-body">
              <p style={{ margin: 0, color: '#475569', fontSize: '0.9rem' }}>
                These custom fields will be rendered on the seller form when &quot;{schemaModalTemplate.name}&quot; is selected:
              </p>

              {parsedModalFields.length === 0 ? (
                <div className="empty-state">No dynamic attributes configured for this template. Standard quantity and condition fields will be used.</div>
              ) : (
                <div className="schema-field-preview">
                  {parsedModalFields.map((f) => (
                    <div key={f.id} className="schema-field-row">
                      <div>
                        <strong>{f.label}</strong> {f.required && <span style={{ color: '#dc2626' }}>*</span>}
                        <div style={{ color: '#64748b', fontSize: '0.78rem' }}>
                          ID: <code>{f.id}</code> &bull; Type: <code>{f.type}</code>
                          {f.unit && ` (${f.unit})`}
                        </div>
                      </div>
                      <div>
                        {f.options && f.options.length > 0 && (
                          <span style={{ fontSize: '0.8rem', color: '#0369a1' }}>
                            Options: {f.options.join(', ')}
                          </span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <div style={{ marginTop: '0.75rem' }}>
                <label style={{ fontSize: '0.8rem', fontWeight: 600, color: '#334155' }}>Raw Schema JSON:</label>
                <pre
                  style={{
                    background: '#f1f5f9',
                    padding: '0.75rem',
                    borderRadius: '8px',
                    fontSize: '0.78rem',
                    maxHeight: '180px',
                    overflowY: 'auto',
                  }}
                >
                  {schemaModalTemplate.attributeSchema}
                </pre>
              </div>
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

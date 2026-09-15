import { FormEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  managerMaterialsApi,
  type ManagerMaterialsApi,
  type MaterialCategory,
} from '../../features/materials/managerMaterialsApi';

export function ManagerCategoriesPage({
  api = managerMaterialsApi,
}: {
  api?: ManagerMaterialsApi;
}) {
  const [categories, setCategories] = useState<MaterialCategory[] | null>(null);
  const [name, setName] = useState('');
  const [editing, setEditing] = useState<MaterialCategory | null>(null);
  const [editName, setEditName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    setError(null);
    try {
      setCategories(await api.getCategories());
    } catch (reason) {
      setError(messageFor(reason));
    }
  };

  useEffect(() => {
    void load();
  }, [api]);

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validName = validateName(name);
    if (!validName) return;
    setSaving(true);
    setError(null);
    try {
      const created = await api.createCategory(validName);
      setCategories((current) => [...(current ?? []), created].sort(byName));
      setName('');
    } catch (reason) {
      setError(messageFor(reason));
    } finally {
      setSaving(false);
    }
  }

  async function update(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editing) return;
    const validName = validateName(editName);
    if (!validName) return;
    setSaving(true);
    setError(null);
    try {
      const updated = await api.updateCategory(editing.id, validName);
      setCategories((current) => (current ?? []).map((category) => category.id === updated.id ? updated : category).sort(byName));
      setEditing(null);
      setEditName('');
    } catch (reason) {
      setError(messageFor(reason));
    } finally {
      setSaving(false);
    }
  }

  async function remove(category: MaterialCategory) {
    if (!window.confirm(`Delete the "${category.name}" category?`)) return;
    setSaving(true);
    setError(null);
    try {
      await api.deleteCategory(category.id);
      setCategories((current) => (current ?? []).filter((item) => item.id !== category.id));
    } catch (reason) {
      setError(messageFor(reason));
    } finally {
      setSaving(false);
    }
  }

  const validation = name && !validateName(name) ? 'Enter a category name of 1 to 120 characters.' : null;
  const editValidation = editName && !validateName(editName) ? 'Enter a category name of 1 to 120 characters.' : null;

  return (
    <div className="manager-page">
      <Link className="back-link" to="/app/manager">Back to Material listings</Link>
      <section className="page-heading">
        <div>
          <p className="eyebrow">Manager workspace</p>
          <h1>Material categories</h1>
          <p className="muted">Keep the shared listing categories accurate and reusable.</p>
        </div>
      </section>

      {error && <p className="error-message" role="alert">{error}</p>}

      <section className="manager-panel">
        <h2>Add category</h2>
        <form className="inline-form" onSubmit={create}>
          <label>
            Category name
            <input
              aria-label="Category name"
              value={name}
              maxLength={120}
              required
              onChange={(event) => setName(event.target.value)}
            />
          </label>
          <button className="button button-primary" type="submit" disabled={saving}>Add category</button>
        </form>
        {validation && <p className="field-error" role="alert">{validation}</p>}
      </section>

      <section className="manager-panel" aria-labelledby="category-list-heading">
        <h2 id="category-list-heading">Current categories</h2>
        {categories === null ? (
          <p aria-live="polite">Loading categories...</p>
        ) : categories.length === 0 ? (
          <p className="empty-state">No categories have been added.</p>
        ) : (
          <ul className="category-list">
            {categories.map((category) => (
              <li key={category.id}>
                {editing?.id === category.id ? (
                  <form className="inline-form" onSubmit={update}>
                    <label>
                      <span className="sr-only">New name for {category.name}</span>
                      <input
                        aria-label={`New name for ${category.name}`}
                        value={editName}
                        maxLength={120}
                        required
                        onChange={(event) => setEditName(event.target.value)}
                      />
                    </label>
                    <button className="button button-primary" type="submit" disabled={saving}>Save</button>
                    <button className="button button-secondary" type="button" onClick={() => setEditing(null)}>Cancel</button>
                  </form>
                ) : (
                  <>
                    <span>{category.name}</span>
                    <div className="action-row">
                      <button className="text-button" type="button" onClick={() => { setEditing(category); setEditName(category.name); }}>Rename</button>
                      <button className="text-button danger-text" type="button" disabled={saving} onClick={() => void remove(category)}>Delete</button>
                    </div>
                  </>
                )}
                {editing?.id === category.id && editValidation && <p className="field-error" role="alert">{editValidation}</p>}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function validateName(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 && trimmed.length <= 120 ? trimmed : null;
}

function byName(left: MaterialCategory, right: MaterialCategory): number {
  return left.name.localeCompare(right.name);
}

function messageFor(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Unable to update material categories.';
}

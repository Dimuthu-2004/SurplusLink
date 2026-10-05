import { useState } from 'react';
import type { ConstructionItemTemplate } from '../api/constructionItemTemplatesApi';

interface ConstructionItemPickerProps {
  templates: ConstructionItemTemplate[];
  selectedTemplate: ConstructionItemTemplate | null;
  onSelectTemplate: (template: ConstructionItemTemplate) => void;
  onSelectCustom: () => void;
  isCustomSelected?: boolean;
}

const itemClasses = [
  ['ALL', 'All Items'],
  ['MATERIAL', 'Materials'],
  ['TOOL', 'Tools'],
  ['EQUIPMENT', 'Equipment'],
  ['FIXTURE', 'Fixtures'],
  ['TEMPORARY_WORK', 'Temporary Works'],
  ['OTHER_CONSTRUCTION', 'Other Construction'],
] as const;

export function ConstructionItemPicker({
  templates,
  selectedTemplate,
  onSelectTemplate,
  onSelectCustom,
  isCustomSelected = false,
}: ConstructionItemPickerProps) {
  const [search, setSearch] = useState('');
  const [itemClass, setItemClass] = useState('ALL');
  const query = search.trim().toLowerCase();
  const filtered = templates.filter(template =>
    template.isActive &&
    (itemClass === 'ALL' || template.itemClass === itemClass) &&
    (!query || `${template.name} ${template.categoryName}`.toLowerCase().includes(query))
  );

  return (
    <section aria-label="Construction item selection" style={{ display: 'grid', gap: '1rem' }}>
      <h2>What are you listing?</h2>
      <label className="catalog-field">
        Search construction items
        <input
          type="search"
          placeholder="Search construction items..."
          value={search}
          onChange={event => setSearch(event.target.value)}
        />
      </label>
      <div role="group" aria-label="Item type" style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
        {itemClasses.map(([value, label]) => (
          <button key={value} type="button" aria-pressed={itemClass === value} onClick={() => setItemClass(value)}>
            {label}
          </button>
        ))}
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0.75rem' }}>
        {filtered.map(template => (
          <button
            key={template.id}
            type="button"
            data-testid={`template-card-${template.id}`}
            aria-pressed={!isCustomSelected && selectedTemplate?.id === template.id}
            onClick={() => onSelectTemplate(template)}
            style={{ display: 'grid', gap: '0.5rem', padding: '1rem', textAlign: 'left' }}
          >
            <strong>{template.name}</strong>
            <span>{template.categoryName}</span>
            <span>{template.packageType ? `Sold in ${template.packageType}s (${template.baseUnit})` : `Sold per ${template.baseUnit}`}</span>
          </button>
        ))}
      </div>
      {filtered.length === 0 && <p>No construction items matching your search.</p>}
      <div>
        <p>Can't find your construction item? List a custom item for manager verification.</p>
        <button type="button" data-testid="custom-item-fallback-btn" aria-pressed={isCustomSelected} onClick={onSelectCustom}>
          Add Custom Item
        </button>
      </div>
    </section>
  );
}

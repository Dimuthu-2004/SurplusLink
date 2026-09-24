import { useId, useState } from 'react';

export function unitLabel(unit: string) {
  return ({ m2: 'm²', m3: 'm³', l: 'L' } as Record<string, string>)[unit] ?? unit;
}

export function UnitMultiSelect({ label, catalog, selected, onChange, disabled }: {
  label: string; catalog: string[]; selected: string[]; onChange: (units: string[]) => void; disabled: boolean;
}) {
  const id = useId();
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const options = catalog.filter(unit => (unit + ' ' + unitLabel(unit)).toLowerCase().includes(query.trim().toLowerCase()));
  function toggle(unit: string) { onChange(selected.includes(unit) ? selected.filter(value => value !== unit) : [...selected, unit]); }
  return <div style={{ minWidth: 'min(18rem, 100%)', flex: '1 1 18rem' }}>
    <label htmlFor={id}>{label}</label>
    <input id={id} role="combobox" aria-autocomplete="list" aria-expanded={open} aria-controls={id + '-options'}
      aria-activedescendant={open && options[active] ? id + '-option-' + active : undefined}
      disabled={disabled} value={query} placeholder="Type to filter units" onFocus={() => setOpen(true)}
      onChange={event => { setQuery(event.target.value); setActive(0); setOpen(true); }}
      onKeyDown={event => {
        if (event.key === 'Escape') setOpen(false);
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
          event.preventDefault(); setOpen(true);
          setActive(index => Math.max(0, Math.min(options.length - 1, index + (event.key === 'ArrowDown' ? 1 : -1))));
        }
        if (event.key === 'Enter') { event.preventDefault(); if (open && options[active]) toggle(options[active]); else setOpen(true); }
      }} />
    {open && <div id={id + '-options'} role="listbox" aria-label={label + ' options'} aria-multiselectable="true" style={{ maxHeight: '12rem', overflowY: 'auto' }}>
      {options.map((unit, index) => <button id={id + '-option-' + index} key={unit} type="button" role="option"
        aria-selected={selected.includes(unit)} disabled={disabled} onClick={() => toggle(unit)} style={{ display: 'block', width: '100%', textAlign: 'left' }}>
        {selected.includes(unit) ? '✓ ' : ''}{unitLabel(unit)}
      </button>)}
      {options.length === 0 && <p>No matching units.</p>}
    </div>}
    <div className="action-row" aria-label={label + ' selected'}>{selected.map(unit => <button type="button" key={unit} disabled={disabled}
      aria-label={'Remove ' + unitLabel(unit)} onClick={() => toggle(unit)}>{unitLabel(unit)} ×</button>)}</div>
  </div>;
}

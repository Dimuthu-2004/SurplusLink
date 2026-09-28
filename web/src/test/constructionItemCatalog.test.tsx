import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ConstructionItemPicker } from '../components/ConstructionItemPicker';
import {
  TemplateDrivenMaterialForm,
  type ListingSubmitData,
} from '../components/TemplateDrivenMaterialForm';
import { ManagerCatalogPage } from '../pages/manager/ManagerCatalogPage';
import type { ConstructionItemTemplate } from '../api/constructionItemTemplatesApi';
import type { MaterialCategory } from '../features/materials/managerMaterialsApi';
import * as templatesApi from '../api/constructionItemTemplatesApi';
import { managerMaterialsApi } from '../features/materials/managerMaterialsApi';

const mockTemplates: ConstructionItemTemplate[] = [
  {
    id: 'paint-id',
    name: 'Paint',
    categoryId: 'finishes-id',
    categoryName: 'Finishes & Paints',
    itemClass: 'MATERIAL',
    quantityMode: 'PACKAGE',
    baseUnit: 'L',
    packageType: 'can',
    allowedUnits: ['L'],
    allowedPackageSizes: [1, 4, 10, 20],
    attributeSchema: JSON.stringify([
      { id: 'colour', label: 'Colour', type: 'string', required: true },
      { id: 'finish', label: 'Finish', type: 'select', options: ['Gloss', 'Matte', 'Satin', 'Semi-Gloss'] },
    ]),
    priceBasis: 'PER_PACKAGE',
    isActive: true,
    createdAtUtc: '2026-09-28T00:00:00Z',
    updatedAtUtc: '2026-09-28T00:00:00Z',
  },
  {
    id: 'generator-id',
    name: 'Generator',
    categoryId: 'power-id',
    categoryName: 'Power & Heavy Machinery',
    itemClass: 'EQUIPMENT',
    quantityMode: 'PIECE',
    baseUnit: 'unit',
    packageType: null,
    allowedUnits: ['unit'],
    allowedPackageSizes: [],
    attributeSchema: JSON.stringify([
      { id: 'capacity_kva', label: 'Capacity (kVA)', type: 'number', required: true, unit: 'kVA' },
      { id: 'fuel_type', label: 'Fuel Type', type: 'select', options: ['Diesel', 'Petrol', 'LPG'] },
      { id: 'phase', label: 'Phase', type: 'select', options: ['Single Phase', 'Three Phase'] },
      { id: 'running_hours', label: 'Running Hours', type: 'number' },
    ]),
    priceBasis: 'PER_UNIT',
    isActive: true,
    createdAtUtc: '2026-09-28T00:00:00Z',
    updatedAtUtc: '2026-09-28T00:00:00Z',
  },
  {
    id: 'sand-id',
    name: 'Sand',
    categoryId: 'aggregates-id',
    categoryName: 'Aggregates & Sand',
    itemClass: 'MATERIAL',
    quantityMode: 'CONTINUOUS_BULK',
    baseUnit: 'Cubes',
    packageType: null,
    allowedUnits: ['Cubes', 'Tons'],
    allowedPackageSizes: [],
    attributeSchema: JSON.stringify([
      { id: 'grade', label: 'Sand Grade', type: 'select', options: ['River Sand', 'Manufactured Sand (M-Sand)', 'Off-shore Washed Sand'] },
    ]),
    priceBasis: 'PER_UNIT',
    isActive: true,
    createdAtUtc: '2026-09-28T00:00:00Z',
    updatedAtUtc: '2026-09-28T00:00:00Z',
  },
];

const mockCategories: MaterialCategory[] = [
  { id: 'finishes-id', name: 'Finishes & Paints', allowedUnits: ['L'], createdAtUtc: '2026-09-28T00:00:00Z', updatedAtUtc: '2026-09-28T00:00:00Z' },
  { id: 'power-id', name: 'Power & Heavy Machinery', allowedUnits: ['unit'], createdAtUtc: '2026-09-28T00:00:00Z', updatedAtUtc: '2026-09-28T00:00:00Z' },
  { id: 'aggregates-id', name: 'Aggregates & Sand', allowedUnits: ['Cubes', 'Tons'], createdAtUtc: '2026-09-28T00:00:00Z', updatedAtUtc: '2026-09-28T00:00:00Z' },
];

describe('ConstructionItemPicker', () => {
  it('renders search and template cards', () => {
    const onSelect = vi.fn();
    const onCustom = vi.fn();

    render(
      <ConstructionItemPicker
        templates={mockTemplates}
        selectedTemplate={null}
        onSelectTemplate={onSelect}
        onSelectCustom={onCustom}
      />
    );

    expect(screen.getByText('What are you listing?')).toBeInTheDocument();
    expect(screen.getByText('Paint')).toBeInTheDocument();
    expect(screen.getByText('Generator')).toBeInTheDocument();
    expect(screen.getByText('Sand')).toBeInTheDocument();
  });

  it('filters templates by item class', () => {
    const onSelect = vi.fn();
    const onCustom = vi.fn();

    render(
      <ConstructionItemPicker
        templates={mockTemplates}
        selectedTemplate={null}
        onSelectTemplate={onSelect}
        onSelectCustom={onCustom}
      />
    );

    fireEvent.click(screen.getByText('Equipment'));
    expect(screen.queryByText('Paint')).not.toBeInTheDocument();
    expect(screen.getByText('Generator')).toBeInTheDocument();
  });

  it('selects template on click', () => {
    const onSelect = vi.fn();
    const onCustom = vi.fn();

    render(
      <ConstructionItemPicker
        templates={mockTemplates}
        selectedTemplate={null}
        onSelectTemplate={onSelect}
        onSelectCustom={onCustom}
      />
    );

    fireEvent.click(screen.getByTestId('template-card-paint-id'));
    expect(onSelect).toHaveBeenCalledWith(mockTemplates[0]);
  });

  it('invokes custom fallback when clicking custom item button', () => {
    const onSelect = vi.fn();
    const onCustom = vi.fn();

    render(
      <ConstructionItemPicker
        templates={mockTemplates}
        selectedTemplate={null}
        onSelectTemplate={onSelect}
        onSelectCustom={onCustom}
      />
    );

    fireEvent.click(screen.getByTestId('custom-item-fallback-btn'));
    expect(onCustom).toHaveBeenCalled();
  });
});

describe('TemplateDrivenMaterialForm', () => {
  it('handles Paint package calculation seamlessly without technical jargon', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <TemplateDrivenMaterialForm
        templates={mockTemplates}
        categories={mockCategories}
        onSubmit={onSubmit}
      />
    );

    // Assert absence of technical jargon
    expect(screen.queryByText(/sellable quantity mode/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/base-equivalent/i)).not.toBeInTheDocument();

    // Select Paint template
    fireEvent.click(screen.getByTestId('template-card-paint-id'));

    // Check dynamic attribute fields rendered
    expect(screen.getByText(/Paint Specifications/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Colour/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Finish/i)).toBeInTheDocument();

    // Enter colour and finish
    fireEvent.change(screen.getByTestId('spec-field-colour'), { target: { value: 'Brilliant White' } });
    fireEvent.change(screen.getByTestId('spec-field-finish'), { target: { value: 'Gloss' } });

    // Packaging: 5 cans of 4L
    fireEvent.change(screen.getByTestId('package-size-select'), { target: { value: '4' } });
    fireEvent.change(screen.getByTestId('package-count-input'), { target: { value: '5' } });
    fireEvent.change(screen.getByTestId('price-per-package-input'), { target: { value: '4500' } });

    // Assert calculated stock summary
    expect(screen.getByText(/Total Sellable Stock:/i)).toBeInTheDocument();
    expect(screen.getByTestId('quantity-summary-box')).toHaveTextContent('20 L');

    // Submit
    fireEvent.click(screen.getByTestId('submit-listing-btn'));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled();
    });

    const submitted: ListingSubmitData = onSubmit.mock.calls[0][0];
    expect(submitted.title).toBe('Paint');
    expect(submitted.quantity).toBe(20); // 5 * 4
    expect(submitted.quantityMode).toBe('PACKAGE');
    expect(submitted.packageSize).toBe(4);
    expect(submitted.packageCount).toBe(5);
    expect(submitted.pricePerPackage).toBe(4500);
    expect(submitted.unitPrice).toBe(1125); // 4500 / 4
    expect(submitted.constructionItemTemplateId).toBe('paint-id');
    expect(JSON.parse(submitted.specificationsJson!)).toEqual({
      colour: 'Brilliant White',
      finish: 'Gloss',
    });
    expect(submitted.isCustomPendingReview).toBe(false);
  });

  it('handles Generator equipment with capacity and unit pricing', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <TemplateDrivenMaterialForm
        templates={mockTemplates}
        categories={mockCategories}
        onSubmit={onSubmit}
      />
    );

    // Select Generator template
    fireEvent.click(screen.getByTestId('template-card-generator-id'));

    expect(screen.getByText(/Generator Specifications/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Capacity \(kVA\)/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Fuel Type/i)).toBeInTheDocument();

    // Enter specs
    fireEvent.change(screen.getByTestId('spec-field-capacity_kva'), { target: { value: '15' } });
    fireEvent.change(screen.getByTestId('spec-field-fuel_type'), { target: { value: 'Diesel' } });
    fireEvent.change(screen.getByTestId('spec-field-phase'), { target: { value: 'Three Phase' } });

    // Piece quantity: 1 generator at 350,000 LKR
    fireEvent.change(screen.getByTestId('piece-count-input'), { target: { value: '1' } });
    fireEvent.change(screen.getByTestId('price-per-piece-input'), { target: { value: '350000' } });

    // Submit
    fireEvent.click(screen.getByTestId('submit-listing-btn'));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled();
    });

    const submitted: ListingSubmitData = onSubmit.mock.calls[0][0];
    expect(submitted.quantity).toBe(1);
    expect(submitted.quantityMode).toBe('PIECE');
    expect(submitted.unitPrice).toBe(350000);
    expect(submitted.packageCount).toBe(1);
    expect(submitted.packageSize).toBe(1);
    expect(submitted.constructionItemTemplateId).toBe('generator-id');
    expect(JSON.parse(submitted.specificationsJson!)).toEqual({
      capacity_kva: 15,
      fuel_type: 'Diesel',
      phase: 'Three Phase',
    });
  });

  it('supports custom unlisted item fallback with manager review flag', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <TemplateDrivenMaterialForm
        templates={mockTemplates}
        categories={mockCategories}
        onSubmit={onSubmit}
      />
    );

    // Click custom item fallback
    fireEvent.click(screen.getByTestId('custom-item-fallback-btn'));

    // Fill custom fields
    fireEvent.change(screen.getByTestId('input-listing-title'), {
      target: { value: 'Custom Plate Compactor' },
    });
    fireEvent.change(screen.getByTestId('select-custom-category'), {
      target: { value: 'power-id' },
    });

    // Custom sale type: Piece
    fireEvent.click(screen.getByTestId('custom-type-piece'));
    fireEvent.change(screen.getByTestId('piece-count-input'), { target: { value: '2' } });
    fireEvent.change(screen.getByTestId('price-per-piece-input'), { target: { value: '85000' } });

    // Submit
    fireEvent.click(screen.getByTestId('submit-listing-btn'));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalled();
    });

    const submitted: ListingSubmitData = onSubmit.mock.calls[0][0];
    expect(submitted.title).toBe('Custom Plate Compactor');
    expect(submitted.isCustomPendingReview).toBe(true);
    expect(submitted.constructionItemTemplateId).toBeNull();
    expect(submitted.quantity).toBe(2);
    expect(submitted.unitPrice).toBe(85000);
  });
});

describe('ManagerCatalogPage', () => {
  beforeEach(() => {
    vi.spyOn(managerMaterialsApi, 'getCategories').mockResolvedValue(mockCategories);
    vi.spyOn(templatesApi, 'fetchItemTemplates').mockResolvedValue(mockTemplates);
    vi.spyOn(templatesApi, 'toggleItemTemplateStatus').mockImplementation(async (id, active) => {
      const found = mockTemplates.find((t) => t.id === id);
      return { ...found!, isActive: active };
    });
  });

  it('renders catalog page with template listings', async () => {
    render(
      <MemoryRouter>
        <ManagerCatalogPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Construction Item Catalog')).toBeInTheDocument();
      expect(screen.getByText('Paint')).toBeInTheDocument();
      expect(screen.getByText('Generator')).toBeInTheDocument();
      expect(screen.getByText('Sand')).toBeInTheDocument();
    });
  });

  it('allows toggling template active status', async () => {
    render(
      <MemoryRouter>
        <ManagerCatalogPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByTestId('toggle-template-paint-id')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('toggle-template-paint-id'));

    await waitFor(() => {
      expect(templatesApi.toggleItemTemplateStatus).toHaveBeenCalledWith('paint-id', false);
    });
  });

  it('opens schema viewer modal on clicking Schema', async () => {
    render(
      <MemoryRouter>
        <ManagerCatalogPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Schema')[0]).toBeInTheDocument();
    });

    fireEvent.click(screen.getAllByText('Schema')[0]);

    await waitFor(() => {
      expect(screen.getByText(/Dynamic Attributes: Paint/i)).toBeInTheDocument();
      expect(screen.getByText('Colour')).toBeInTheDocument();
    });
  });
});

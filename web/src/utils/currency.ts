const lkrNumber = new Intl.NumberFormat('en-LK', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatLkr(value: number | null | undefined): string {
  return value == null || !Number.isFinite(value) ? 'Not available' : `LKR ${lkrNumber.format(value)}`;
}

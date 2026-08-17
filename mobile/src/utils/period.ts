import type {PeriodTypeKey} from '../config';

export function formatPeriodLabel(period: string, periodType: PeriodTypeKey): string {
  if (periodType === 'daily') {
    const [, m, d] = period.split('-');
    return `${d}/${m}`;
  }
  if (periodType === 'monthly') {
    const [y, m] = period.split('-');
    return `Th${m}/${y.slice(2)}`;
  }
  if (periodType === 'quarterly') {
    const [y, q] = period.split('-');
    return `${q}/${y.slice(2)}`;
  }
  return period;
}

export function compactNumber(n: number): string {
  if (n >= 1e9) return `${trimZero(n / 1e9)}tỷ`;
  if (n >= 1e6) return `${trimZero(n / 1e6)}tr`;
  if (n >= 1e3) return `${trimZero(n / 1e3)}k`;
  return String(Math.round(n));
}

function trimZero(n: number): string {
  return n.toFixed(1).replace(/\.0$/, '');
}

// Peak-End avgEmotion from the API is -1..1; the app always displays it as a friendlier 0-100 index.
export function emotionIndex(raw: number): number {
  return Math.max(0, Math.min(100, Math.round(((raw || 0) + 1) / 2 * 100)));
}

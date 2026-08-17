// Change this to your dev machine's LAN IP when testing on a physical phone with Expo Go —
// the phone can't reach 127.0.0.1 (that's the phone itself). Both devices must be on the
// same Wi-Fi, and the backend must be started with AISLE_HOST=0.0.0.0 (see mobile/README.md).
export const API_BASE_URL = 'http://192.168.1.27:8765';

export const PERIOD_TYPES = [
  {key: 'daily', label: 'Ngày'},
  {key: 'monthly', label: 'Tháng'},
  {key: 'quarterly', label: 'Quý'},
  {key: 'yearly', label: 'Năm'},
] as const;

export type PeriodTypeKey = (typeof PERIOD_TYPES)[number]['key'];

export const DEFAULT_PERIOD_TYPE: PeriodTypeKey = 'monthly';

// Only the most recent N points are charted for the finer-grained views, so the screen
// doesn't have to render (and the phone doesn't have to scroll through) unbounded history.
export const MAX_POINTS: Record<PeriodTypeKey, number> = {
  daily: 30,
  monthly: 24,
  quarterly: 12,
  yearly: 10,
};

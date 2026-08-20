export const API_BASE_URL = 'http://10.251.11.36:8765';

export const ALIASES_API = {
  thang: 'thang',
  quy: 'quy',
  nam: 'nam',
} as const;

export const STATISTIC_TYPES = [
  { key: 'thang', label: 'Tháng' },
  { key: 'quy', label: 'Quý' },
  { key: 'nam', label: 'Năm' },
] as const;

export type StatisticTypeKey = (typeof STATISTIC_TYPES)[number]['key'];

export const MIN_YEAR = 2000;

export function currentYear(): number {
  return new Date().getFullYear();
}

export const CHARTS_PALETTE = [
  '#F97316',
  '#3B82F6',
  '#10B981',
  '#EF4444',
  '#8B5CF6',
  '#F59E0B',
  '#06B6D4',
  '#EC4899',
  '#84CC16',
  '#64748B',
  '#F43F5E',
  '#14B8A6',
];
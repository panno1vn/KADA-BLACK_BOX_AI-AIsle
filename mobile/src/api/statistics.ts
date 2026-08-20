import { API_BASE_URL } from '../config';
import type { StatisticsResponse } from '../types';

export function fetchStatistics(type: string, year: number): Promise<StatisticsResponse> {
  const url = `${API_BASE_URL}/statistics-by/${encodeURIComponent(type)}/${year}`;
  return new Promise((resolve, reject) => {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 10000);
    fetch(url, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`API ${response.status}: ${response.statusText}`);
        }
        return response.json() as Promise<StatisticsResponse>;
      })
      .then(resolve)
      .catch((e) => reject(e instanceof Error ? e : new Error('Không thể tải dữ liệu')))
      .finally(() => clearTimeout(timer));
  });
}
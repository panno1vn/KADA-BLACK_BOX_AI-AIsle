import {API_BASE_URL} from '../config';
import type {AnalyticsResponse} from '../types';

export function fetchAnalytics(): Promise<AnalyticsResponse> {
  const url = `${API_BASE_URL}/api/analytics`;
  return new Promise((resolve, reject) => {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 10000);
    fetch(url, {signal: controller.signal})
      .then((response) => {
        if (!response.ok) throw new Error(`API ${response.status}: ${response.statusText}`);
        return response.json() as Promise<AnalyticsResponse>;
      })
      .then(resolve)
      .catch((e) => reject(e instanceof Error ? e : new Error('Không thể tải dữ liệu')))
      .finally(() => clearTimeout(timer));
  });
}

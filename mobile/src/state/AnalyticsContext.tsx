import {createContext, useCallback, useContext, useEffect, useState, type ReactNode} from 'react';
import {fetchAnalytics} from '../api/analytics';
import type {AnalyticsResponse} from '../types';

// Fetched once here and shared across every tab, so switching tabs doesn't re-hit the API —
// each MetricScreen just reads the same snapshot and picks its own field out of it.
interface AnalyticsState {
  data: AnalyticsResponse | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

const AnalyticsContext = createContext<AnalyticsState | null>(null);

export function AnalyticsProvider({children}: {children: ReactNode}) {
  const [data, setData] = useState<AnalyticsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await fetchAnalytics());
    } catch (e) {
      setData(null);
      setError(e instanceof Error ? e.message : 'Không thể tải dữ liệu');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return <AnalyticsContext.Provider value={{data, loading, error, refresh}}>{children}</AnalyticsContext.Provider>;
}

export function useAnalytics(): AnalyticsState {
  const ctx = useContext(AnalyticsContext);
  if (!ctx) throw new Error('useAnalytics must be used within an AnalyticsProvider');
  return ctx;
}

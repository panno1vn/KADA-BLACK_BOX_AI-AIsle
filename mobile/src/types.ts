// Mirrors backend/analytics.mjs's buildAnalytics() output (GET /api/analytics).
export interface AnalyticsBucket {
  runs: number;
  revenue: number;
  purchases: number;
  customersIn: number;
  customersOut: number;
  converted: number;
  mainBuyers: number;
  impulseBuyers: number;
  conversionRate: number; // 0..1
  mainRate: number; // 0..1
  impulseRate: number; // 0..1
  avgEmotion: number; // -1..1, Peak-End average per NPC
}

export interface AnalyticsSeriesPoint extends AnalyticsBucket {
  period: string; // 'YYYY-MM-DD' | 'YYYY-MM' | 'YYYY-Qn' | 'YYYY'
}

export interface AnalyticsResponse {
  totals: AnalyticsBucket;
  series: {
    daily: AnalyticsSeriesPoint[];
    monthly: AnalyticsSeriesPoint[];
    quarterly: AnalyticsSeriesPoint[];
    yearly: AnalyticsSeriesPoint[];
  };
}

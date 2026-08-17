export interface StatisticSlice {
  key: number;
  value: string;
}

export interface StatisticsResponse {
  percent: StatisticSlice[];
  numberOfPurchases: number;
}
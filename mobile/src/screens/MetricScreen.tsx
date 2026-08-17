import {useMemo, useState} from 'react';
import {ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import {DEFAULT_PERIOD_TYPE, MAX_POINTS, type PeriodTypeKey} from '../config';
import {useAnalytics} from '../state/AnalyticsContext';
import {formatPeriodLabel} from '../utils/period';
import PeriodSegment from '../components/PeriodSegment';
import MetricBarChart from '../components/MetricBarChart';
import type {AnalyticsBucket} from '../types';

export interface MetricSeriesConfig {
  key: keyof AnalyticsBucket;
  label: string;
  color: string;
  transform?: (raw: number) => number;
}

export interface MetricConfig {
  title: string;
  totalLabel: string;
  headlineValue: (totals: AnalyticsBucket) => string;
  series: MetricSeriesConfig[];
  formatValue: (value: number) => string;
  formatYLabel?: (value: number) => string;
}

export default function MetricScreen({metric}: {metric: MetricConfig}) {
  const {data, loading, error, refresh} = useAnalytics();
  const [periodType, setPeriodType] = useState<PeriodTypeKey>(DEFAULT_PERIOD_TYPE);
  const [refreshing, setRefreshing] = useState(false);

  const onRefresh = async () => {
    setRefreshing(true);
    await refresh();
    setRefreshing(false);
  };

  const chart = useMemo(() => {
    const full = data?.series?.[periodType] ?? [];
    const points = full.slice(-MAX_POINTS[periodType]);
    return {
      periods: points.map((p) => formatPeriodLabel(p.period, periodType)),
      series: metric.series.map((s) => ({
        label: s.label,
        color: s.color,
        values: points.map((p) => {
          const raw = Number(p[s.key]) || 0;
          return s.transform ? s.transform(raw) : raw;
        }),
      })),
    };
  }, [data, periodType, metric]);

  return (
    <SafeAreaView style={styles.safe} edges={['top', 'left', 'right']}>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#3B82F6" />}
      >
        <Text style={styles.screenTitle}>{metric.title}</Text>

        {loading && !data ? (
          <View style={styles.centerBox}>
            <ActivityIndicator size="large" color="#3B82F6" />
            <Text style={styles.hint}>Đang tải dữ liệu...</Text>
          </View>
        ) : error && !data ? (
          <View style={styles.centerBox}>
            <Text style={styles.errorText}>{error}</Text>
            <Pressable style={styles.retryButton} onPress={refresh}>
              <Text style={styles.retryText}>Thử lại</Text>
            </Pressable>
          </View>
        ) : !data || data.totals.runs === 0 ? (
          <View style={styles.centerBox}>
            <Text style={styles.hint}>Chưa có dữ liệu mô phỏng nào được lưu.</Text>
          </View>
        ) : (
          <>
            <View style={styles.card}>
              <Text style={styles.cardLabel}>{metric.totalLabel}</Text>
              <Text style={styles.cardValue}>{metric.headlineValue(data.totals)}</Text>
            </View>

            <PeriodSegment value={periodType} onChange={setPeriodType} />

            <View style={styles.card}>
              <MetricBarChart
                periods={chart.periods}
                series={chart.series}
                formatValue={metric.formatValue}
                formatYLabel={metric.formatYLabel}
              />
            </View>
          </>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: '#F1F5F9'},
  content: {padding: 16, gap: 16},
  screenTitle: {fontSize: 22, fontWeight: '700', color: '#0F172A'},
  centerBox: {alignItems: 'center', paddingVertical: 48, gap: 12},
  hint: {fontSize: 13, color: '#64748B', textAlign: 'center'},
  errorText: {fontSize: 14, color: '#DC2626', textAlign: 'center'},
  retryButton: {backgroundColor: '#3B82F6', borderRadius: 8, paddingHorizontal: 18, paddingVertical: 10},
  retryText: {color: '#FFFFFF', fontWeight: '600'},
  card: {backgroundColor: '#FFFFFF', borderRadius: 14, padding: 16, gap: 8},
  cardLabel: {fontSize: 13, color: '#64748B'},
  cardValue: {fontSize: 28, fontWeight: '800', color: '#0F172A'},
});

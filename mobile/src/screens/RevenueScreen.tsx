import { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { currentYear, STATISTIC_TYPES, StatisticTypeKey } from '../config';
import { fetchStatistics } from '../api/statistics';
import type { StatisticsResponse } from '../types';
import YearPicker from '../components/YearPicker';
import StatPieChart from '../components/StatPieChart';

function labelFor(type: StatisticTypeKey, key: number): string {
  switch (type) {
    case 'thang':
      return `Tháng ${key}`;
    case 'quy':
      return `Quý ${key}`;
    default:
      return `Năm ${key}`;
  }
}

export default function RevenueScreen() {
  const [year, setYear] = useState<number>(currentYear());
  const [type, setType] = useState<StatisticTypeKey>('quy');
  const [data, setData] = useState<StatisticsResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [pickerVisible, setPickerVisible] = useState<boolean>(false);

  const load = useCallback(
    async (activeType: StatisticTypeKey, activeYear: number) => {
      setLoading(true);
      setError(null);
      try {
        const result = await fetchStatistics(activeType, activeYear);
        setData(result);
      } catch (e) {
        setData(null);
        setError(e instanceof Error ? e.message : 'Không thể tải dữ liệu');
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  useEffect(() => {
    load(type, year);
  }, [type, year, load]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await load(type, year);
    setRefreshing(false);
  }, [type, year, load]);

  return (
    <SafeAreaView style={styles.safe} edges={['top', 'left', 'right']}>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#3B82F6" />
        }
      >
        <Text style={styles.screenTitle}>Thống kê doanh thu</Text>

        <Pressable style={styles.yearButton} onPress={() => setPickerVisible(true)}>
          <Ionicons name="calendar-outline" size={18} color="#0F172A" />
          <Text style={styles.yearButtonText}>Năm {year}</Text>
          <Ionicons name="chevron-down" size={16} color="#64748B" />
        </Pressable>

        <View style={styles.segment}>
          {STATISTIC_TYPES.map((option) => {
            const active = option.key === type;
            return (
              <Pressable
                key={option.key}
                style={[styles.segmentItem, active && styles.segmentItemActive]}
                onPress={() => setType(option.key)}
              >
                <Text style={[styles.segmentText, active && styles.segmentTextActive]}>
                  {option.label}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {loading ? (
          <View style={styles.centerBox}>
            <ActivityIndicator size="large" color="#3B82F6" />
            <Text style={styles.hint}>Đang tải dữ liệu...</Text>
          </View>
        ) : error ? (
          <View style={styles.centerBox}>
            <Text style={styles.errorText}>{error}</Text>
            <Pressable style={styles.retryButton} onPress={() => load(type, year)}>
              <Text style={styles.retryText}>Thử lại</Text>
            </Pressable>
          </View>
        ) : data ? (
          <>
            <View style={styles.card}>
              <Text style={styles.cardLabel}>Tổng lượt mua trong năm</Text>
              <Text style={styles.cardValue}>{data.numberOfPurchases}</Text>
            </View>

            <View style={styles.card}>
              <StatPieChart
                slices={data.percent.map((slice) => ({
                  key: slice.key,
                  value: parseFloat(String(slice.value).replace('%', '')),
                }))}
                labelFor={(key) => labelFor(type, key)}
              />
            </View>
          </>
        ) : null}

        <YearPicker
          value={year}
          visible={pickerVisible}
          onChange={setYear}
          onClose={() => setPickerVisible(false)}
        />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: '#F1F5F9',
  },
  content: {
    padding: 16,
    gap: 16,
  },
  screenTitle: {
    fontSize: 22,
    fontWeight: '700',
    color: '#0F172A',
  },
  yearButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: '#CBD5E1',
    paddingHorizontal: 14,
    paddingVertical: 12,
    alignSelf: 'flex-start',
  },
  yearButtonText: {
    fontSize: 15,
    fontWeight: '600',
    color: '#0F172A',
  },
  segment: {
    flexDirection: 'row',
    backgroundColor: '#E2E8F0',
    borderRadius: 10,
    padding: 4,
  },
  segmentItem: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: 9,
    borderRadius: 8,
  },
  segmentItemActive: {
    backgroundColor: '#FFFFFF',
  },
  segmentText: {
    fontSize: 14,
    color: '#475569',
  },
  segmentTextActive: {
    color: '#2563EB',
    fontWeight: '700',
  },
  centerBox: {
    alignItems: 'center',
    paddingVertical: 48,
    gap: 12,
  },
  hint: {
    fontSize: 13,
    color: '#64748B',
  },
  errorText: {
    fontSize: 14,
    color: '#DC2626',
    textAlign: 'center',
  },
  retryButton: {
    backgroundColor: '#3B82F6',
    borderRadius: 8,
    paddingHorizontal: 18,
    paddingVertical: 10,
  },
  retryText: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 14,
    padding: 16,
    gap: 8,
  },
  cardLabel: {
    fontSize: 13,
    color: '#64748B',
  },
  cardValue: {
    fontSize: 28,
    fontWeight: '800',
    color: '#0F172A',
  },
});
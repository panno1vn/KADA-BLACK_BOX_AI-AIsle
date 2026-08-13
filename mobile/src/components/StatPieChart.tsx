import { StyleSheet, Text, View } from 'react-native';
import { PieChart } from 'react-native-gifted-charts';
import { CHARTS_PALETTE } from '../config';

export interface SliceValue {
  key: number;
  value: number;
}

interface StatPieChartProps {
  slices: SliceValue[];
  labelFor: (key: number) => string;
}

function formatPercent(value: number): string {
  const rounded = Math.round(value * 10) / 10;
  return `${Number.isInteger(rounded) ? rounded.toFixed(0) : rounded.toFixed(1)}%`;
}

export default function StatPieChart({ slices, labelFor }: StatPieChartProps) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);
  const data =
    total > 0
      ? slices.map((slice, index) => ({
          value: slice.value,
          color: CHARTS_PALETTE[index % CHARTS_PALETTE.length],
          text: `${Math.round(slice.value)}%`,
        }))
      : [];

  return (
    <View style={styles.container}>
      {data.length > 0 ? (
        <PieChart
          data={data}
          showText
          textColor="#FFFFFF"
          textSize={11}
          radius={110}
          innerRadius={0}
          strokeColor="#FFFFFF"
          strokeWidth={1.5}
        />
      ) : (
        <View style={styles.empty}>
          <Text style={styles.emptyText}>Chưa có dữ liệu</Text>
        </View>
      )}
      <View style={styles.legend}>
        {slices.map((slice, index) => (
          <View key={slice.key} style={styles.legendRow}>
            <View
              style={[
                styles.legendDot,
                { backgroundColor: CHARTS_PALETTE[index % CHARTS_PALETTE.length] },
              ]}
            />
            <Text style={styles.legendLabel}>{labelFor(slice.key)}</Text>
            <Text style={styles.legendValue}>{formatPercent(slice.value)}</Text>
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    gap: 16,
  },
  empty: {
    width: 220,
    height: 220,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyText: {
    color: '#94A3B8',
    fontSize: 14,
  },
  legend: {
    alignSelf: 'stretch',
    gap: 8,
  },
  legendRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  legendDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
  },
  legendLabel: {
    flex: 1,
    fontSize: 14,
    color: '#334155',
  },
  legendValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#0F172A',
  },
});
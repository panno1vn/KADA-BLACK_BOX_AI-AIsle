import {useState} from 'react';
import {StyleSheet, Text, View} from 'react-native';
import {BarChart} from 'react-native-gifted-charts';

interface Series {
  label: string;
  color: string;
  values: number[]; // one value per entry in `periods`, same order/length
}

interface MetricBarChartProps {
  periods: string[]; // already-formatted, short period labels (e.g. "Th08/26")
  series: Series[]; // 1 series = plain bars; 2 series = grouped bars (e.g. Khách vào/ra)
  formatYLabel?: (value: number) => string;
  formatValue?: (value: number) => string;
}

const HEIGHT = 220;

export default function MetricBarChart({periods, series, formatYLabel, formatValue}: MetricBarChartProps) {
  const [selected, setSelected] = useState<number | null>(null);

  if (!periods.length || !series.length) {
    return (
      <View style={styles.empty}>
        <Text style={styles.emptyText}>Chưa đủ dữ liệu theo kỳ này.</Text>
      </View>
    );
  }

  const grouped = series.length > 1;
  const data = periods.flatMap((period, i) =>
    series.map((s, seriesIndex) => ({
      value: s.values[i] ?? 0,
      frontColor: s.color,
      label: grouped ? (seriesIndex === 0 ? period : '') : period,
      spacing: grouped ? (seriesIndex === series.length - 1 ? 18 : 2) : 14,
      labelTextStyle: styles.axisLabel,
      onPress: () => setSelected(i),
      topLabelComponent: undefined,
    })),
  );

  const activeIndex = selected ?? periods.length - 1;

  return (
    <View>
      {series.length > 1 && (
        <View style={styles.legend}>
          {series.map((s) => (
            <View key={s.label} style={styles.legendRow}>
              <View style={[styles.legendDot, {backgroundColor: s.color}]} />
              <Text style={styles.legendLabel}>{s.label}</Text>
            </View>
          ))}
        </View>
      )}

      <View style={styles.readout}>
        <Text style={styles.readoutPeriod}>{periods[activeIndex]}</Text>
        <View style={styles.readoutValues}>
          {series.map((s) => (
            <Text key={s.label} style={[styles.readoutValue, {color: s.color}]}>
              {series.length > 1 ? `${s.label}: ` : ''}
              {formatValue ? formatValue(s.values[activeIndex] ?? 0) : String(s.values[activeIndex] ?? 0)}
            </Text>
          ))}
        </View>
      </View>

      <BarChart
        data={data}
        height={HEIGHT}
        barWidth={grouped ? 14 : 22}
        barBorderRadius={4}
        yAxisThickness={0}
        xAxisThickness={1}
        xAxisColor="#CBD5E1"
        yAxisTextStyle={styles.axisLabel}
        rulesColor="#E2E8F0"
        rulesType="solid"
        formatYLabel={formatYLabel ? (label) => formatYLabel(Number(label)) : undefined}
        noOfSections={4}
        isAnimated
        animationDuration={250}
        scrollToEnd
        initialSpacing={12}
        endSpacing={12}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  empty: {
    height: HEIGHT,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyText: {
    color: '#94A3B8',
    fontSize: 14,
  },
  axisLabel: {
    color: '#64748B',
    fontSize: 10,
  },
  legend: {
    flexDirection: 'row',
    gap: 16,
    marginBottom: 8,
  },
  legendRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  legendDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  legendLabel: {
    fontSize: 12,
    color: '#475569',
  },
  readout: {
    marginBottom: 10,
  },
  readoutPeriod: {
    fontSize: 12,
    color: '#94A3B8',
  },
  readoutValues: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
    marginTop: 2,
  },
  readoutValue: {
    fontSize: 18,
    fontWeight: '700',
  },
});

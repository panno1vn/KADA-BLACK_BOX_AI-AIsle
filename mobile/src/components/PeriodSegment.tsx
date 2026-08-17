import {Pressable, StyleSheet, Text, View} from 'react-native';
import {PERIOD_TYPES, type PeriodTypeKey} from '../config';

interface PeriodSegmentProps {
  value: PeriodTypeKey;
  onChange: (value: PeriodTypeKey) => void;
}

export default function PeriodSegment({value, onChange}: PeriodSegmentProps) {
  return (
    <View style={styles.segment}>
      {PERIOD_TYPES.map((option) => {
        const active = option.key === value;
        return (
          <Pressable
            key={option.key}
            style={[styles.item, active && styles.itemActive]}
            onPress={() => onChange(option.key)}
          >
            <Text style={[styles.text, active && styles.textActive]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  segment: {
    flexDirection: 'row',
    backgroundColor: '#E2E8F0',
    borderRadius: 10,
    padding: 4,
  },
  item: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: 9,
    borderRadius: 8,
  },
  itemActive: {
    backgroundColor: '#FFFFFF',
  },
  text: {
    fontSize: 14,
    color: '#475569',
  },
  textActive: {
    color: '#2563EB',
    fontWeight: '700',
  },
});

import { useMemo } from 'react';
import {
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { currentYear, MIN_YEAR } from '../config';

interface YearPickerProps {
  value: number;
  onChange: (year: number) => void;
  visible: boolean;
  onClose: () => void;
}

export default function YearPicker({ value, onChange, visible, onClose }: YearPickerProps) {
  const years = useMemo(() => {
    const list: number[] = [];
    for (let y = currentYear(); y >= MIN_YEAR; y -= 1) list.push(y);
    return list;
  }, []);

  const indexOfValue = years.indexOf(value);

  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose}>
        <Pressable style={styles.sheet} onPress={() => undefined}>
          <View style={styles.header}>
            <Text style={styles.title}>Chọn năm</Text>
            <Pressable onPress={onClose} hitSlop={12}>
              <Text style={styles.close}>Đóng</Text>
            </Pressable>
          </View>
          <FlatList
            data={years}
            keyExtractor={(item) => String(item)}
            initialScrollIndex={indexOfValue >= 0 ? indexOfValue : undefined}
            initialNumToRender={8}
            maxToRenderPerBatch={12}
            windowSize={5}
            renderItem={({ item }) => {
              const selected = item === value;
              return (
                <Pressable
                  style={[styles.row, selected && styles.rowSelected]}
                  onPress={() => {
                    onChange(item);
                    onClose();
                  }}
                >
                  <Text style={[styles.rowText, selected && styles.rowTextSelected]}>
                    {item}
                  </Text>
                </Pressable>
              );
            }}
            ListFooterComponent={<View style={styles.footer} />}
          />
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.45)',
    justifyContent: 'flex-end',
  },
  sheet: {
    maxHeight: '70%',
    backgroundColor: '#ffffff',
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    paddingHorizontal: 8,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: '#E2E8F0',
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
    color: '#0F172A',
  },
  close: {
    fontSize: 14,
    color: '#3B82F6',
    fontWeight: '600',
  },
  row: {
    paddingVertical: 14,
    paddingHorizontal: 16,
    borderRadius: 8,
  },
  rowSelected: {
    backgroundColor: '#EFF6FF',
  },
  rowText: {
    fontSize: 15,
    color: '#334155',
  },
  rowTextSelected: {
    color: '#2563EB',
    fontWeight: '700',
  },
  footer: {
    height: 24,
  },
});
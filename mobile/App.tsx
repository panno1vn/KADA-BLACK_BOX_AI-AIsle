import {NavigationContainer} from '@react-navigation/native';
import {createBottomTabNavigator} from '@react-navigation/bottom-tabs';
import {Ionicons} from '@expo/vector-icons';
import {StatusBar} from 'expo-status-bar';
import {AnalyticsProvider} from './src/state/AnalyticsContext';
import MetricScreen, {type MetricConfig} from './src/screens/MetricScreen';
import {money, count, percent, scoreOn100} from './src/utils/format';
import {emotionIndex} from './src/utils/period';

const BLUE = '#3B82F6';
const ORANGE = '#F97316';

// Same 5 metrics as the web "Thống Kê" dashboard, backed by the same GET /api/analytics.
const METRICS: Record<string, MetricConfig> = {
  revenue: {
    title: 'Doanh thu',
    totalLabel: 'Tổng doanh thu',
    headlineValue: (t) => money(t.revenue),
    series: [{key: 'revenue', label: 'Doanh thu', color: BLUE}],
    formatValue: money,
  },
  customers: {
    title: 'Khách vào / ra',
    totalLabel: 'Khách vào / ra',
    headlineValue: (t) => `${count(t.customersIn)} / ${count(t.customersOut)}`,
    series: [
      {key: 'customersIn', label: 'Khách vào', color: BLUE},
      {key: 'customersOut', label: 'Khách ra', color: ORANGE},
    ],
    formatValue: count,
  },
  purchases: {
    title: 'Lượt mua',
    totalLabel: 'Tổng lượt mua',
    headlineValue: (t) => count(t.purchases),
    series: [{key: 'purchases', label: 'Lượt mua', color: BLUE}],
    formatValue: count,
  },
  conversion: {
    title: 'Tỉ lệ chuyển đổi',
    totalLabel: 'Tỉ lệ chuyển đổi trung bình',
    headlineValue: (t) => percent(t.conversionRate * 100),
    series: [{key: 'conversionRate', label: 'Tỉ lệ chuyển đổi', color: BLUE, transform: (raw) => raw * 100}],
    formatValue: percent,
  },
  emotion: {
    title: 'Cảm xúc khách hàng',
    totalLabel: 'Chỉ số cảm xúc trung bình',
    headlineValue: (t) => scoreOn100(emotionIndex(t.avgEmotion)),
    series: [{key: 'avgEmotion', label: 'Cảm xúc', color: BLUE, transform: emotionIndex}],
    formatValue: scoreOn100,
  },
};

const Tab = createBottomTabNavigator();

const ICONS: Record<string, {active: string; inactive: string}> = {
  revenue: {active: 'stats-chart', inactive: 'stats-chart-outline'},
  customers: {active: 'people', inactive: 'people-outline'},
  purchases: {active: 'cart', inactive: 'cart-outline'},
  conversion: {active: 'checkmark-circle', inactive: 'checkmark-circle-outline'},
  emotion: {active: 'happy', inactive: 'happy-outline'},
};

export default function App() {
  return (
    <AnalyticsProvider>
      <NavigationContainer>
        <StatusBar style="auto" />
        <Tab.Navigator
          screenOptions={{
            headerShown: true,
            tabBarActiveTintColor: '#2563EB',
            tabBarInactiveTintColor: '#94A3B8',
            headerTitleAlign: 'center',
          }}
        >
          {Object.entries(METRICS).map(([key, metric]) => (
            <Tab.Screen
              key={key}
              name={key}
              options={{
                title: metric.title,
                tabBarLabel: metric.title,
                tabBarIcon: ({color, size, focused}) => (
                  <Ionicons name={(focused ? ICONS[key].active : ICONS[key].inactive) as never} size={size} color={color} />
                ),
              }}
            >
              {() => <MetricScreen metric={metric} />}
            </Tab.Screen>
          ))}
        </Tab.Navigator>
      </NavigationContainer>
    </AnalyticsProvider>
  );
}

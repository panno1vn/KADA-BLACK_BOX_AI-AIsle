import { NavigationContainer } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { StatusBar } from 'expo-status-bar';
import RevenueScreen from './src/screens/RevenueScreen';
import PurchasesScreen from './src/screens/PurchasesScreen';

const Tab = createBottomTabNavigator();

export default function App() {
  return (
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
        <Tab.Screen
          name="DoanhThu"
          component={RevenueScreen}
          options={{
            title: 'Doanh thu',
            tabBarLabel: 'Doanh thu',
            tabBarIcon: ({ color, size, focused }) => (
              <Ionicons name={focused ? 'stats-chart' : 'stats-chart-outline'} size={size} color={color} />
            ),
          }}
        />
        <Tab.Screen
          name="MuaHang"
          component={PurchasesScreen}
          options={{
            title: 'Mua hàng',
            tabBarLabel: 'Mua hàng',
            tabBarIcon: ({ color, size, focused }) => (
              <Ionicons name={focused ? 'cart' : 'cart-outline'} size={size} color={color} />
            ),
          }}
        />
      </Tab.Navigator>
    </NavigationContainer>
  );
}
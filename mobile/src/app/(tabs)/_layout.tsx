import React from "react";
import { Tabs } from "expo-router";
import { Home, Pill, Settings } from "lucide-react-native";
import { colors } from "@/theme";

export default function TabsLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.primaryDark,
        tabBarInactiveTintColor: colors.inkMuted,
        tabBarLabelStyle: { fontSize: 13, fontWeight: "600", marginBottom: 4 },
        tabBarStyle: {
          minHeight: 64,
          paddingTop: 8,
          paddingBottom: 10,
          backgroundColor: colors.surface,
          borderTopColor: colors.border,
        },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: "Dashboard",
          tabBarAccessibilityLabel: "Dashboard",
          tabBarIcon: ({ color, size }) => <Home color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="medications"
        options={{
          title: "Medications",
          tabBarAccessibilityLabel: "My medications",
          tabBarIcon: ({ color, size }) => <Pill color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="settings"
        options={{
          title: "Settings",
          tabBarAccessibilityLabel: "Settings",
          tabBarIcon: ({ color, size }) => <Settings color={color} size={size} />,
        }}
      />
    </Tabs>
  );
}

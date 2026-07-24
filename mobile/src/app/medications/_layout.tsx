import { Stack } from "expo-router";
import { colors } from "@/theme";

export default function MedicationsStackLayout() {
  return (
    <Stack
      screenOptions={{
        headerTintColor: colors.primaryDark,
        headerTitleStyle: { fontWeight: "700", fontSize: 18 },
        contentStyle: { backgroundColor: colors.muted },
      }}
    >
      <Stack.Screen name="add" options={{ title: "Add medication" }} />
      <Stack.Screen name="[id]" options={{ title: "Medication" }} />
      <Stack.Screen name="edit/[id]" options={{ title: "Edit medication" }} />
    </Stack>
  );
}

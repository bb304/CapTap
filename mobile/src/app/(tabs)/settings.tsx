import React from "react";
import { Alert, Pressable, StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { ChevronRight } from "lucide-react-native";
import { Button, Card, Screen, SectionHeader } from "@/components/ui";
import { useAuth } from "@/context/AuthContext";
import { routes } from "@/constants/routes";
import { colors, spacing, touchTarget, typography } from "@/theme";

const rows = [
  {
    id: "history",
    title: "Dose history",
    hint: "Your recent medication logs",
    route: routes.history,
  },
  {
    id: "scan",
    title: "Scan bottle tag",
    hint: "NFC tap to confirm a dose",
    route: routes.nfcScan,
  },
  {
    id: "notifications",
    title: "Notifications",
    hint: "Reminders & quiet hours",
    route: routes.notificationSettings,
  },
  { id: "theme", title: "Theme", hint: "Light (default)" },
  { id: "privacy", title: "Privacy", hint: "How CapTap protects your data" },
  { id: "security", title: "Security", hint: "Sessions & lockout" },
  { id: "about", title: "About CapTap", hint: "Tap. Confirm. Peace of mind." },
] as const;

export default function SettingsScreen() {
  const { user, signOut } = useAuth();
  const router = useRouter();

  return (
    <Screen>
      <SectionHeader title="Settings" subtitle={user?.email ?? "Signed in"} />
      <Card style={styles.list}>
        {rows.map((row, index) => (
          <Pressable
            key={row.id}
            onPress={() => {
              if ("route" in row && row.route) {
                router.push(row.route);
                return;
              }
              Alert.alert(row.title, "This setting will connect in a later phase.");
            }}
            accessibilityRole="button"
            accessibilityLabel={row.title}
            style={[styles.row, index < rows.length - 1 ? styles.divider : null]}
          >
            <View style={styles.copy}>
              <Text style={styles.title} maxFontSizeMultiplier={1.4}>
                {row.title}
              </Text>
              <Text style={styles.hint} maxFontSizeMultiplier={1.4}>
                {row.hint}
              </Text>
            </View>
            <ChevronRight color={colors.inkMuted} size={22} />
          </Pressable>
        ))}
      </Card>
      <Button
        label="Sign out"
        variant="secondary"
        onPress={async () => {
          await signOut();
          router.replace(routes.welcome);
        }}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  list: { paddingVertical: 0, paddingHorizontal: 0, overflow: "hidden" },
  row: {
    minHeight: touchTarget + 12,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
  },
  divider: {
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  copy: { flex: 1, gap: 2 },
  title: { ...typography.label, fontSize: 18 },
  hint: { ...typography.caption },
});

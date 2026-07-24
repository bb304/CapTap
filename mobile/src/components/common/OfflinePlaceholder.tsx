import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { WifiOff } from "lucide-react-native";
import { colors, spacing, typography } from "@/theme";

export function OfflinePlaceholder() {
  return (
    <View style={styles.wrap} accessibilityRole="summary" accessibilityLabel="You appear to be offline">
      <WifiOff color={colors.inkMuted} size={36} />
      <Text style={styles.title} maxFontSizeMultiplier={1.5}>
        You're offline
      </Text>
      <Text style={styles.body} maxFontSizeMultiplier={1.5}>
        CapTap will sync when you're back online. Your local reminders still matter.
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: "center",
    gap: spacing.md,
    padding: spacing.xl,
  },
  title: { ...typography.subtitle, textAlign: "center" },
  body: { ...typography.bodyMuted, textAlign: "center" },
});

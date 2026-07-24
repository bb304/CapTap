import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { CloudOff } from "lucide-react-native";
import { usePendingSyncCount } from "@/hooks/useMedicationLogs";
import { colors, spacing, typography } from "@/theme";

/** Compact banner when there are pending offline queue items. */
export function OfflineSyncBanner() {
  const pending = usePendingSyncCount();
  const count = pending.data ?? 0;
  if (count <= 0) return null;

  return (
    <View
      style={styles.wrap}
      accessibilityRole="summary"
      accessibilityLabel={`${count} doses waiting to sync`}
    >
      <CloudOff color={colors.primaryDark} size={18} />
      <Text style={styles.text} maxFontSizeMultiplier={1.4}>
        {count === 1
          ? "1 dose saved offline — will sync when you're back online."
          : `${count} doses saved offline — will sync when you're back online.`}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm,
    padding: spacing.md,
    marginBottom: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
  },
  text: { ...typography.caption, flex: 1, color: colors.ink },
});

import React from "react";
import { StyleSheet, Text, View } from "react-native";
import type { AdherenceStatus } from "@/types/medication";
import { colors, radius, spacing, typography } from "@/theme";

const statusColors: Record<AdherenceStatus, { bg: string; fg: string }> = {
  Upcoming: { bg: colors.primarySoft, fg: colors.primaryDark },
  Due: { bg: "#FFF4E5", fg: colors.warning },
  Taken: { bg: "#E6F6EE", fg: colors.success },
  Missed: { bg: "#FCEAEA", fg: colors.danger },
};

type BadgeProps = {
  status: AdherenceStatus;
};

export function Badge({ status }: BadgeProps) {
  const tone = statusColors[status];
  return (
    <View
      style={[styles.badge, { backgroundColor: tone.bg }]}
      accessibilityLabel={`Status ${status}`}
    >
      <Text style={[styles.text, { color: tone.fg }]} maxFontSizeMultiplier={1.4}>
        {status}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    alignSelf: "flex-start",
    borderRadius: radius.full,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
  text: {
    ...typography.caption,
    fontWeight: "700",
  },
});

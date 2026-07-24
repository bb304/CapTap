import React from "react";
import { Pressable, StyleSheet, Text } from "react-native";
import { colors, radius, spacing, touchTarget, typography } from "@/theme";

type ChipProps = {
  label: string;
  selected?: boolean;
  onPress?: () => void;
};

export function Chip({ label, selected = false, onPress }: ChipProps) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityState={{ selected }}
      accessibilityLabel={label}
      style={[styles.chip, selected ? styles.selected : null]}
    >
      <Text style={[styles.label, selected ? styles.labelSelected : null]} maxFontSizeMultiplier={1.4}>
        {label}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  chip: {
    minHeight: touchTarget,
    borderRadius: radius.full,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.lg,
    alignItems: "center",
    justifyContent: "center",
  },
  selected: {
    backgroundColor: colors.primarySoft,
    borderColor: colors.primary,
  },
  label: {
    ...typography.label,
    color: colors.inkMuted,
  },
  labelSelected: {
    color: colors.primaryDark,
  },
});

import React from "react";
import { ActivityIndicator, StyleSheet, Text, View } from "react-native";
import { colors, spacing, typography } from "@/theme";

type LoadingSpinnerProps = {
  label?: string;
};

export function LoadingSpinner({ label = "Loading" }: LoadingSpinnerProps) {
  return (
    <View style={styles.wrap} accessibilityRole="progressbar" accessibilityLabel={label}>
      <ActivityIndicator size="large" color={colors.primary} />
      <Text style={styles.label} maxFontSizeMultiplier={1.5}>
        {label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.md,
    padding: spacing.xl,
  },
  label: {
    ...typography.bodyMuted,
  },
});
